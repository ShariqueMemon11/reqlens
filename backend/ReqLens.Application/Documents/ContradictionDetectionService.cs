using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NJsonSchema.Validation;
using ReqLens.Application.Llm;
using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public sealed class ContradictionDetectionService : IContradictionDetectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDocumentRepository _documents;
    private readonly IEmbeddingClient _embeddings;
    private readonly ILlmClient _llm;
    private readonly ContradictionDetectionOptions _options;
    private readonly ILogger<ContradictionDetectionService> _logger;
    private readonly JsonSchema _schema;

    public ContradictionDetectionService(
        IDocumentRepository documents,
        IEmbeddingClient embeddings,
        ILlmClient llm,
        IOptions<ContradictionDetectionOptions> options,
        ILogger<ContradictionDetectionService> logger)
    {
        _documents = documents;
        _embeddings = embeddings;
        _llm = llm;
        _options = options.Value;
        _logger = logger;
        _schema = JsonSchema.FromJsonAsync(ContradictionsJsonSchema.Json).GetAwaiter().GetResult();
    }

    public async Task<DetectContradictionsResult> DetectAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Document not found.");

        var requirements = await _documents.GetRequirementsAsync(documentId, cancellationToken);
        if (requirements.Count < 2)
        {
            await _documents.ReplaceContradictionsAsync(documentId, [], cancellationToken);
            return EmptyResult(documentId, 0, 0);
        }

        var vectors = await _embeddings.EmbedAsync(
            requirements.Select(r => r.Text).ToList(),
            cancellationToken);

        if (vectors.Count != requirements.Count)
        {
            throw new LlmException(502, "Embedding count did not match requirement count.");
        }

        var (sent, aboveThreshold) = SelectNeighborCandidates(requirements, vectors);
        var batchSize = Math.Max(1, _options.MaxCandidatePairs);
        var batches = (int)Math.Ceiling(sent.Count / (double)batchSize);

        _logger.LogInformation(
            "Contradiction candidates: {AboveThreshold} pairs at or above cosine {Threshold}; sending {Sent} unique top-{K} neighbor pairs to the LLM ({Batches} batch(es) of up to {BatchSize}).",
            aboveThreshold,
            _options.SimilarityThreshold,
            sent.Count,
            _options.NeighborsPerRequirement,
            Math.Max(batches, sent.Count == 0 ? 0 : 1),
            batchSize);

        if (sent.Count == 0)
        {
            await _documents.ReplaceContradictionsAsync(documentId, [], cancellationToken);
            return EmptyResult(documentId, aboveThreshold, 0);
        }

        var knownIds = requirements.Select(r => r.RequirementId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entities = new List<Contradiction>();
        foreach (var batch in sent.Chunk(batchSize))
        {
            entities.AddRange(await JudgeAsync(batch.ToList(), knownIds, cancellationToken));
        }

        entities = entities
            .GroupBy(entity => (entity.RequirementIdA, entity.RequirementIdB))
            .Select(group => group.First())
            .ToList();

        foreach (var entity in entities)
        {
            entity.DocumentId = document.Id;
        }

        await _documents.ReplaceContradictionsAsync(document.Id, entities, cancellationToken);

        return new DetectContradictionsResult(
            document.Id,
            entities.Select(ToDto).ToList(),
            CandidateCapReached: false,
            aboveThreshold,
            sent.Count,
            _options.SimilarityThreshold,
            _options.NeighborsPerRequirement);
    }

    private async Task<List<Contradiction>> JudgeAsync(
        IReadOnlyList<CandidatePair> batch,
        HashSet<string> knownIds,
        CancellationToken cancellationToken)
    {
        var messages = new List<LlmMessage>
        {
            new("system", SystemPrompt),
            new("user", UserPrompt(batch))
        };

        var raw = await _llm.CompleteJsonAsync(LlmRequest(messages), cancellationToken);
        var (dto, errors) = Validate(raw);
        if (dto is null)
        {
            messages.Add(new("assistant", raw));
            messages.Add(new("user", RepairPrompt(errors, raw)));
            raw = await _llm.CompleteJsonAsync(LlmRequest(messages), cancellationToken);
            (dto, errors) = Validate(raw);
        }

        if (dto is null)
        {
            throw new LlmException(
                422,
                "The model returned contradictions that did not match the schema after one repair attempt. " + errors);
        }

        return dto.Contradictions
            .Select(item => ToEntity(item, knownIds))
            .OfType<Contradiction>()
            .ToList();
    }

    private (List<CandidatePair> Sent, int AboveThreshold) SelectNeighborCandidates(
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<float[]> vectors)
    {
        var count = requirements.Count;
        var neighborLists = Enumerable.Range(0, count).Select(_ => new List<(int Index, double Similarity)>()).ToArray();
        var aboveThreshold = 0;

        for (var i = 0; i < count; i++)
        {
            for (var j = i + 1; j < count; j++)
            {
                if (requirements[i].SourceLine == requirements[j].SourceLine)
                {
                    continue;
                }

                var similarity = CosineSimilarity.Compute(vectors[i], vectors[j]);
                if (similarity < _options.SimilarityThreshold)
                {
                    continue;
                }

                aboveThreshold++;
                neighborLists[i].Add((j, similarity));
                neighborLists[j].Add((i, similarity));
            }
        }

        var k = Math.Max(1, _options.NeighborsPerRequirement);
        var seen = new HashSet<(string, string)>();
        var sent = new List<CandidatePair>();

        for (var i = 0; i < count; i++)
        {
            foreach (var neighbor in neighborLists[i].OrderByDescending(item => item.Similarity).Take(k))
            {
                var left = requirements[i];
                var right = requirements[neighbor.Index];
                var key = string.Compare(left.RequirementId, right.RequirementId, StringComparison.OrdinalIgnoreCase) <= 0
                    ? (left.RequirementId, right.RequirementId)
                    : (right.RequirementId, left.RequirementId);
                if (!seen.Add(key))
                {
                    continue;
                }

                sent.Add(new CandidatePair(left, right, neighbor.Similarity));
            }
        }

        return (
            sent.OrderByDescending(pair => pair.Similarity).ToList(),
            aboveThreshold);
    }

    private DetectContradictionsResult EmptyResult(Guid documentId, int aboveThreshold, int sent) =>
        new(
            documentId,
            [],
            false,
            aboveThreshold,
            sent,
            _options.SimilarityThreshold,
            _options.NeighborsPerRequirement);

    private (ContradictionsDetectionDto? Dto, string Errors) Validate(string raw)
    {
        var errors = _schema.Validate(raw, new JsonSchemaValidatorSettings());
        if (errors.Count > 0)
        {
            return (null, string.Join("; ", errors.Select(e => e.ToString())));
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ContradictionsDetectionDto>(raw, JsonOptions);
            return dto is null ? (null, "Response deserialized to null.") : (dto, "");
        }
        catch (JsonException ex)
        {
            return (null, ex.Message);
        }
    }

    private static Contradiction? ToEntity(ContradictionItemDto item, HashSet<string> knownIds)
    {
        var ids = item.RequirementIds
            .Where(id => knownIds.Contains(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count != 2)
        {
            return null;
        }

        var description = item.Description.Trim();
        var question = item.ResolutionQuestion.Trim();
        if (description.Length == 0 || question.Length == 0)
        {
            return null;
        }

        return new Contradiction
        {
            Id = Guid.NewGuid(),
            RequirementIdA = ids[0],
            RequirementIdB = ids[1],
            Description = description,
            ResolutionQuestion = question
        };
    }

    private static ContradictionDto ToDto(Contradiction entity) =>
        AnalysisMapper.ToContradictionDto(entity);

    private static LlmJsonRequest LlmRequest(IReadOnlyList<LlmMessage> messages) =>
        new(messages, ContradictionsJsonSchema.Json, ContradictionsJsonSchema.Name, ReasoningEffort: "low");

    private const string SystemPrompt =
        """
        You judge whether candidate requirement pairs actually contradict.
        A contradiction means both cannot be true as written (conflicting rules, not merely related topics).
        Do not flag two operations on the same actor (create vs edit) as a contradiction.
        Do not flag a split of one sentence into atomic units as a contradiction.
        If a pair is related but compatible, omit it.
        description is a short statement of the conflict.
        resolution_question is one concrete question asking which rule should take precedence.
        Only use the requirement ids you were given.
        If none contradict, return {"contradictions":[]}.
        """;

    private static string UserPrompt(IReadOnlyList<CandidatePair> pairs)
    {
        var blocks = pairs.Select((pair, index) =>
            $"""
                Pair {index + 1} (cosine {pair.Similarity:0.00}):
                {pair.Left.RequirementId}: {pair.Left.Text}
                {pair.Right.RequirementId}: {pair.Right.Text}
                """);
        return $"""
            Judge these candidate pairs. Return only genuine contradictions.

            {string.Join("\n\n", blocks)}
            """;
    }

    private static string RepairPrompt(string errors, string previousJson) =>
        $"""
        Your previous JSON failed schema validation.

        Validation errors:
        {errors}

        Previous JSON:
        {previousJson}

        Return corrected JSON that matches the schema exactly.
        An empty contradictions array is valid.
        """;

    private sealed record CandidatePair(Requirement Left, Requirement Right, double Similarity);
}
