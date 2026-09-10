using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Validation;
using ReqLens.Application.Llm;
using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public sealed class AmbiguityDetectionService : IAmbiguityDetectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDocumentRepository _documents;
    private readonly ILlmClient _llm;
    private readonly JsonSchema _schema;

    public AmbiguityDetectionService(IDocumentRepository documents, ILlmClient llm)
    {
        _documents = documents;
        _llm = llm;
        _schema = JsonSchema.FromJsonAsync(AmbiguitiesJsonSchema.Json).GetAwaiter().GetResult();
    }

    public async Task<DetectAmbiguitiesResult> DetectAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Document not found.");

        var requirements = await _documents.GetRequirementsAsync(documentId, cancellationToken);
        if (requirements.Count == 0)
        {
            await _documents.ReplaceAmbiguitiesAsync(documentId, [], cancellationToken);
            return new DetectAmbiguitiesResult(documentId, []);
        }

        var knownIds = requirements.Select(r => r.RequirementId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var messages = new List<LlmMessage>
        {
            new("system", SystemPrompt),
            new("user", UserPrompt(requirements))
        };

        var raw = await _llm.CompleteJsonAsync(
            AmbiguityLlmRequest(messages),
            cancellationToken);

        var (dto, errors) = Validate(raw);
        if (dto is null)
        {
            messages.Add(new("assistant", raw));
            messages.Add(new("user", RepairPrompt(errors, raw)));
            raw = await _llm.CompleteJsonAsync(
                AmbiguityLlmRequest(messages),
                cancellationToken);
            (dto, errors) = Validate(raw);
        }

        if (dto is null)
        {
            throw new LlmException(
                422,
                "The model returned ambiguities that did not match the schema after one repair attempt. " + errors);
        }

        var entities = dto.Ambiguities
            .Where(item => knownIds.Contains(item.RequirementId))
            .Select(ToEntity)
            .Where(entity => entity.Questions.Count > 0)
            .ToList();

        foreach (var entity in entities)
        {
            entity.DocumentId = document.Id;
        }

        await _documents.ReplaceAmbiguitiesAsync(document.Id, entities, cancellationToken);

        return new DetectAmbiguitiesResult(
            document.Id,
            entities.Select(AnalysisMapper.ToAmbiguityDto).ToList());
    }

    private (AmbiguitiesDetectionDto? Dto, string Errors) Validate(string raw)
    {
        var errors = _schema.Validate(raw, new JsonSchemaValidatorSettings());
        if (errors.Count > 0)
        {
            return (null, string.Join("; ", errors.Select(e => e.ToString())));
        }

        try
        {
            var dto = JsonSerializer.Deserialize<AmbiguitiesDetectionDto>(raw, JsonOptions);
            return dto is null ? (null, "Response deserialized to null.") : (dto, "");
        }
        catch (JsonException ex)
        {
            return (null, ex.Message);
        }
    }

    private static Ambiguity ToEntity(AmbiguityItemDto item)
    {
        var severity = item.Severity.ToLowerInvariant() switch
        {
            "high" => AmbiguitySeverity.High,
            "medium" => AmbiguitySeverity.Medium,
            _ => AmbiguitySeverity.Low
        };

        var ambiguity = new Ambiguity
        {
            Id = Guid.NewGuid(),
            RequirementId = item.RequirementId,
            Severity = severity,
            Issue = item.Issue.Trim()
        };

        var order = 0;
        foreach (var question in item.Questions)
        {
            var text = question.Text.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            ambiguity.Questions.Add(new AmbiguityQuestion
            {
                Id = Guid.NewGuid(),
                Text = text,
                OptionType = "checkbox",
                SortOrder = order++
            });
        }

        return ambiguity;
    }

    private const string AmbiguityReasoningEffort = "low";

    private static LlmJsonRequest AmbiguityLlmRequest(IReadOnlyList<LlmMessage> messages) =>
        new(messages, AmbiguitiesJsonSchema.Json, AmbiguitiesJsonSchema.Name, ReasoningEffort: AmbiguityReasoningEffort);

    private const string SystemPrompt =
        """
        You are a requirements gate, not a completeness auditor.
        Flag a requirement only when a developer would have to guess the core behavior before they can start — "must clarify before development."
        Apply every FLAG rule on its own.

        FLAG:
        - Missing human actor on a permissioned action: a person/role must perform it, but none is named. The object of the action (e.g. "users") is not an actor. "The system shall allow to create…" with no role is this case.
        - Vague catch-all verb with no operation list: manage, handle, support, process (and similar) when the allowed actions are not named.
        - Missing operational constraints that change the feature: file size, file type, file count, rate limits, numeric thresholds (N failed attempts), quotas. Example: "Users can upload attachments to a ticket" — flag size/type/count.
        - Direct conflict: two requirements in this list cannot both be true as written. Put the ambiguity on the later id and name the earlier id in issue.

        DO NOT FLAG as undefined actor:
        - Automated behavior whose actor is the system (lock, notify, email, expire, calculate, record in an audit log, timestamps). "The system" is the correct actor. Do not suggest Admin/User.
        - A human-performed clause that already names the role.

        DO NOT FLAG as a gap:
        - Exact error-message wording, UI layout, log-line format, retry copy, or other presentation detail.
        - Exhaustive success/failure prose when the actor, operations, and operational constraints above are already named.

        Prefer omitting a weak issue over inventing one. If none meet the bar, return {"ambiguities":[]}.

        severity is high, medium, or low based on how much of the core behavior is left undefined.
        issue is a short statement of what is undefined (the core gap only).
        For a missing human actor, issue should say the actor is undefined; questions[].text should be role labels such as "Admin", "Manager", "End user".
        For missing upload/file constraints, questions[].text should be labels such as "Max file size", "Allowed types", "Max file count".
        For a vague catch-all verb, questions[].text MUST be distinct concrete operations, never one umbrella category. Example: "Login", "Logout", "Password reset" — not a single chip "Authentication methods".
        Each questions[].text MUST be a short 2–4 word option label, not a full sentence.
        Never write questions like "Can the admin create new users?".
        option_type is always checkbox.
        Only emit ambiguities for the requirement ids you were given.
        """;

    private static string UserPrompt(IReadOnlyList<Requirement> requirements)
    {
        var lines = string.Join(
            "\n",
            requirements.Select(r => $"{r.RequirementId}: {r.Text}"));
        return $"""
            For each requirement, apply every FLAG rule independently.

            Example FLAG (vague verb): "Admin should be able to manage users." — manage has no operation list. Options: "Create", "Edit", "Deactivate" — not "User management".
            Example FLAG (vague verb): "The system should support user authentication." — support has no operation list. Options: "Login", "Logout", "Password reset" — not "Authentication methods".
            Example FLAG (no human actor): "The system shall allow to create, edit, deactivate, delete, and reset passwords for users, with all actions logged." — no person/role.
            Example FLAG (missing constraints): "Users can upload attachments to a ticket." — size/type/count unspecified.
            Example DO NOT FLAG (human + operations): "The system shall allow an admin to create, edit, deactivate, delete, and reset passwords for users, with all actions logged."
            Example DO NOT FLAG (system actor): "The system notifies the user via email." — automated; do not flag actor-undefined.
            Example FLAG (threshold, not actor): "The system locks the account after failed attempts." — actor is the system; flag the missing count, not Admin/User.

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
        Keep the same FLAG / DO NOT FLAG bar — do not add issues to make the JSON valid.
        An empty ambiguities array is valid.
        questions[].text must remain short 2–4 word option labels, not full sentences.
        Vague-verb options must stay distinct operations, not one umbrella category.
        """;
}
