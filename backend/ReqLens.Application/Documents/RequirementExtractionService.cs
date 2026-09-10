using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Validation;
using ReqLens.Application.Llm;
using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public sealed class RequirementExtractionService : IRequirementExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDocumentRepository _documents;
    private readonly ILlmClient _llm;
    private readonly JsonSchema _schema;

    public RequirementExtractionService(IDocumentRepository documents, ILlmClient llm)
    {
        _documents = documents;
        _llm = llm;
        _schema = JsonSchema.FromJsonAsync(RequirementsJsonSchema.Json).GetAwaiter().GetResult();
    }

    public async Task<ExtractRequirementsResult> ExtractAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Document not found.");

        var source = RequirementCoverage.Inventory(document.ExtractedText);
        var messages = new List<LlmMessage>
        {
            new("system", SystemPrompt),
            new("user", UserPrompt(document.ExtractedText))
        };

        var raw = await _llm.CompleteJsonAsync(
            new LlmJsonRequest(messages, RequirementsJsonSchema.Json, RequirementsJsonSchema.Name),
            cancellationToken);

        var (dto, errors) = Validate(raw);
        if (dto is null)
        {
            messages.Add(new("assistant", raw));
            messages.Add(new("user", RepairPrompt(errors, raw)));
            raw = await _llm.CompleteJsonAsync(
                new LlmJsonRequest(messages, RequirementsJsonSchema.Json, RequirementsJsonSchema.Name),
                cancellationToken);
            (dto, errors) = Validate(raw);
        }

        if (dto is null)
        {
            throw new LlmException(
                422,
                "The model returned requirements that did not match the schema after one repair attempt. " + errors);
        }

        dto = await RecoverUncoveredAsync(messages, dto, source, cancellationToken);
        AppendMissingLines(dto, source);

        var entities = dto.Requirements.Select(item => new Requirement
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            RequirementId = item.Id,
            Text = item.Text.Trim(),
            SourceLine = item.SourceLine > 0 ? item.SourceLine : 1
        }).ToList();

        await _documents.ReplaceRequirementsAsync(document, entities, cancellationToken);

        return new ExtractRequirementsResult(
            document.Id,
            entities.Select(r => new ExtractedRequirementDto(r.RequirementId, r.Text, r.SourceLine)).ToList());
    }

    private async Task<RequirementsExtractionDto> RecoverUncoveredAsync(
        List<LlmMessage> messages,
        RequirementsExtractionDto dto,
        IReadOnlyList<RequirementCoverage.SourceLine> source,
        CancellationToken cancellationToken)
    {
        var reported = dto.Requirements.Select(r => r.SourceLine);
        var missing = RequirementCoverage.MissingLines(source, reported);
        var partial = RequirementCoverage.UnderCoveredLines(source, reported);
        if (missing.Count == 0 && partial.Count == 0)
        {
            return dto;
        }

        var previous = JsonSerializer.Serialize(dto);
        messages.Add(new("assistant", previous));
        messages.Add(new("user", CoverageRepairPrompt(missing, partial, previous)));
        var raw = await _llm.CompleteJsonAsync(
            new LlmJsonRequest(messages, RequirementsJsonSchema.Json, RequirementsJsonSchema.Name),
            cancellationToken);
        var (repaired, _) = Validate(raw);
        return repaired ?? dto;
    }

    private static void AppendMissingLines(
        RequirementsExtractionDto dto,
        IReadOnlyList<RequirementCoverage.SourceLine> source)
    {
        // Only lines the model reported no source_line for — never append when it already
        // emitted units for that line (rewording is not a drop).
        var missing = RequirementCoverage.MissingLines(source, dto.Requirements.Select(r => r.SourceLine));
        foreach (var gap in missing)
        {
            dto.Requirements.Add(new RequirementItemDto
            {
                Id = RequirementCoverage.NextRequirementId(dto.Requirements.Select(r => r.Id)),
                Text = gap.Source.Text,
                SourceLine = gap.Source.Line
            });
        }
    }

    private (RequirementsExtractionDto? Dto, string Errors) Validate(string raw)
    {
        var errors = _schema.Validate(raw, new JsonSchemaValidatorSettings());
        if (errors.Count > 0)
        {
            return (null, string.Join("; ", errors.Select(e => e.ToString())));
        }

        try
        {
            var dto = JsonSerializer.Deserialize<RequirementsExtractionDto>(raw, JsonOptions);
            if (dto is null)
            {
                return (null, "Response deserialized to null.");
            }

            return (dto, "");
        }
        catch (JsonException ex)
        {
            return (null, ex.Message);
        }
    }

    private const string SystemPrompt =
        """
        You split a requirements document into discrete requirement units.
        Each item is a single testable obligation (who + what), not a heading.
        Do not skip an obligation because it is vague, poorly written, or uses words like "properly" / "manage" — those must still become units.
        When a sentence lists several actions, split them, and copy shared context into each unit:
        - Copy the human actor only onto clauses that person actually performs (create/edit/delete).
        - Automatic side effects (audit logging, notifications, timestamps, derived records) are performed by the system, not the human. Write those units with "the system" as the actor. Example: "Managers can create, edit, and delete accounts, with each action recorded in the audit log" → "Managers can create accounts", "Managers can edit accounts", "Managers can delete accounts", "The system records each account action in the audit log".
        Coordinated obligations in one sentence are separate units. Example: "The system shall lock a user account after 5 failed login attempts within 10 minutes, and notify the user via email" → lock unit AND notify-via-email unit. Never keep only the first half.
        Subjectless obligations still become units; you may insert "the system" as actor. That rewording still belongs to the same source_line.
        Assign ids sequentially as R-001, R-002, and so on.
        source_line MUST be the integer in [N] on the source line you consumed. Emit as many units as there are obligation clauses on that line, all with that same source_line.
        """;

    private static string UserPrompt(string documentText) =>
        $"""
        Extract discrete requirements from this numbered document. Represent every obligation clause. source_line is the [N] label.

        {RequirementCoverage.NumberedDocument(documentText)}
        """;

    private static string RepairPrompt(string errors, string previousJson) =>
        $"""
        Your previous JSON failed schema validation.

        Validation errors:
        {errors}

        Previous JSON:
        {previousJson}

        Return corrected JSON that matches the schema exactly.
        """;

    private static string CoverageRepairPrompt(
        IReadOnlyList<RequirementCoverage.LineGap> missing,
        IReadOnlyList<RequirementCoverage.LineGap> partial,
        string previousJson)
    {
        var missingBlock = missing.Count == 0
            ? "(none)"
            : string.Join("\n", missing.Select(g =>
                $"- line {g.Source.Line} (0 units, expected {g.Source.ExpectedClauses}): {g.Source.Text}"));
        var partialBlock = partial.Count == 0
            ? "(none)"
            : string.Join("\n", partial.Select(g =>
                $"- line {g.Source.Line} ({g.Produced} units, expected {g.Source.ExpectedClauses}): {g.Source.Text}"));
        return $"""
            Coverage is counted from the source_line values you already emitted, not from wording.
            Keep every existing unit. Do not add a second copy of a line you already covered, even if you reworded it.
            For missing lines (0 units), add units. For under-covered lines, add only the missing clauses.

            Missing lines:
            {missingBlock}

            Under-covered lines:
            {partialBlock}

            Previous JSON:
            {previousJson}
            """;
    }
}
