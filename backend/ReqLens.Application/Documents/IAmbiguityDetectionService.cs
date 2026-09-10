namespace ReqLens.Application.Documents;

public sealed record AmbiguityQuestionDto(string Text, string OptionType);

public sealed record AmbiguityDto(
    Guid Id,
    string RequirementId,
    string Severity,
    string Issue,
    IReadOnlyList<AmbiguityQuestionDto> Questions,
    bool IsResolved,
    IReadOnlyList<string> SelectedOptions);

public sealed record DetectAmbiguitiesResult(
    Guid DocumentId,
    IReadOnlyList<AmbiguityDto> Ambiguities);

public interface IAmbiguityDetectionService
{
    Task<DetectAmbiguitiesResult> DetectAsync(Guid documentId, CancellationToken cancellationToken);
}
