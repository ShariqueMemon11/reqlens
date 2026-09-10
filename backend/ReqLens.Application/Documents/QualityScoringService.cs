using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Validation;
using ReqLens.Application.Llm;
using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public sealed class QualityScoringService : IQualityScoringService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDocumentRepository _documents;
    private readonly ILlmClient _llm;
    private readonly JsonSchema _schema;

    public QualityScoringService(IDocumentRepository documents, ILlmClient llm)
    {
        _documents = documents;
        _llm = llm;
        _schema = JsonSchema.FromJsonAsync(QualityScoresJsonSchema.Json).GetAwaiter().GetResult();
    }

    public async Task<ComputeQualityScoreResult> ScoreAsync(
        Guid documentId,
        CancellationToken cancellationToken) =>
        await ScoreAsync(documentId, reuseStoredLlmScores: false, cancellationToken);

    public async Task<ComputeQualityScoreResult> ScoreAsync(
        Guid documentId,
        bool reuseStoredLlmScores,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Document not found.");

        var requirements = await _documents.GetRequirementsAsync(documentId, cancellationToken);
        var ambiguities = await _documents.GetAmbiguitiesAsync(documentId, cancellationToken);
        var contradictions = await _documents.GetContradictionsAsync(documentId, cancellationToken);

        var openAmbiguities = ambiguities.Where(item => !item.IsResolved).ToList();
        var openContradictions = contradictions.Where(item => !item.IsResolved).ToList();
        var questionCount = openAmbiguities.Sum(item => item.Questions.Count);
        var signals = QualityScoreCalculator.ComputeSignals(
            requirements.Select(r => r.RequirementId).ToList(),
            requirements.Select(r => r.Text).ToList(),
            openContradictions.Select(c => (c.RequirementIdA, c.RequirementIdB)).ToList(),
            questionCount);

        var llmClarity = 0;
        var llmSpecificity = 0;
        if (requirements.Count > 0)
        {
            if (reuseStoredLlmScores)
            {
                var stored = await _documents.GetQualityScoreAsync(documentId, cancellationToken);
                if (stored is null)
                {
                    (llmClarity, llmSpecificity) = await JudgeWordingAsync(requirements, cancellationToken);
                }
                else
                {
                    llmClarity = stored.LlmClarity;
                    llmSpecificity = stored.LlmSpecificity;
                }
            }
            else
            {
                (llmClarity, llmSpecificity) = await JudgeWordingAsync(requirements, cancellationToken);
            }
        }

        var bars = QualityScoreCalculator.Combine(signals, llmClarity, llmSpecificity);
        var entity = ToEntity(document.Id, signals, bars);
        await _documents.ReplaceQualityScoreAsync(document.Id, entity, cancellationToken);
        return ToResult(document.Id, signals, bars);
    }

    private async Task<(int Clarity, int Specificity)> JudgeWordingAsync(
        IReadOnlyList<Requirement> requirements,
        CancellationToken cancellationToken)
    {
        var messages = new List<LlmMessage>
        {
            new("system", SystemPrompt),
            new("user", UserPrompt(requirements))
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
                "The model returned quality scores that did not match the schema after one repair attempt. " + errors);
        }

        return (
            QualityScoreCalculator.ToScore(dto.Clarity),
            QualityScoreCalculator.ToScore(dto.Specificity));
    }

    private (QualityLlmScoresDto? Dto, string Errors) Validate(string raw)
    {
        var errors = _schema.Validate(raw, new JsonSchemaValidatorSettings());
        if (errors.Count > 0)
        {
            return (null, string.Join("; ", errors.Select(e => e.ToString())));
        }

        try
        {
            var dto = JsonSerializer.Deserialize<QualityLlmScoresDto>(raw, JsonOptions);
            return dto is null ? (null, "Response deserialized to null.") : (dto, "");
        }
        catch (JsonException ex)
        {
            return (null, ex.Message);
        }
    }

    private static QualityScore ToEntity(Guid documentId, QualityRuleSignals signals, QualityBars bars) =>
        new()
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Overall = bars.Overall,
            Completeness = bars.Completeness,
            Clarity = bars.Clarity,
            Testability = bars.Testability,
            Consistency = bars.Consistency,
            Specificity = bars.Specificity,
            RequirementCount = signals.RequirementCount,
            VagueCount = signals.VagueCount,
            VagueScore = signals.VagueScore,
            HasActorCount = signals.HasActorCount,
            ActorScore = signals.ActorScore,
            HasMeasurableConstraintCount = signals.HasMeasurableConstraintCount,
            MeasurableConstraintCoverage = signals.MeasurableConstraintCoverage,
            UnresolvedContradictionCount = signals.UnresolvedContradictionCount,
            InvolvedInContradictionCount = signals.InvolvedInContradictionCount,
            UnansweredQuestionCount = signals.UnansweredQuestionCount,
            QuestionScore = signals.QuestionScore,
            LlmClarity = bars.LlmClarity,
            LlmSpecificity = bars.LlmSpecificity
        };

    private static ComputeQualityScoreResult ToResult(
        Guid documentId,
        QualityRuleSignals signals,
        QualityBars bars) =>
        new(
            documentId,
            bars.Overall,
            bars.Completeness,
            bars.Clarity,
            bars.Testability,
            bars.Consistency,
            bars.Specificity,
            new QualitySignalsDto(
                signals.RequirementCount,
                signals.VagueCount,
                signals.VagueScore,
                signals.HasActorCount,
                signals.ActorScore,
                signals.HasMeasurableConstraintCount,
                signals.MeasurableConstraintCoverage,
                signals.UnresolvedContradictionCount,
                signals.InvolvedInContradictionCount,
                signals.UnansweredQuestionCount,
                signals.QuestionScore,
                bars.LlmClarity,
                bars.LlmSpecificity));

    private static LlmJsonRequest LlmRequest(IReadOnlyList<LlmMessage> messages) =>
        new(messages, QualityScoresJsonSchema.Json, QualityScoresJsonSchema.Name, ReasoningEffort: "low");

    private const string SystemPrompt =
        """
        You score requirement wording quality. Do not re-check actors, numeric bounds, or contradictions — those are scored in code.
        clarity: how easy the wording is to understand (0-100 integer).
        specificity: how precise the language is, ignoring whether a vague verb has an operation list (0-100 integer).
        Return only JSON.
        """;

    private static string UserPrompt(IReadOnlyList<Requirement> requirements)
    {
        var lines = string.Join(
            "\n",
            requirements.Select(r => $"{r.RequirementId}: {r.Text}"));
        return $"""
            Score clarity and specificity for this requirement set as a whole.

            {lines}
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
        clarity and specificity must be integers from 0 to 100.
        """;
}
