namespace ReqLens.Application.Documents;

public sealed record ResolveAmbiguityRequest(IReadOnlyList<string> SelectedOptions);

public sealed record ResolveContradictionRequest(string ChosenRequirementId);

public sealed record ResolveIssueResult(
    Guid DocumentId,
    IReadOnlyList<ExtractedRequirementDto> Requirements,
    IReadOnlyList<AmbiguityDto> Ambiguities,
    IReadOnlyList<ContradictionDto> Contradictions,
    ComputeQualityScoreResult Quality);

public interface IIssueResolutionService
{
    Task<ResolveIssueResult> ResolveAmbiguityAsync(
        Guid documentId,
        Guid ambiguityId,
        ResolveAmbiguityRequest request,
        CancellationToken cancellationToken);

    Task<ResolveIssueResult> ResolveContradictionAsync(
        Guid documentId,
        Guid contradictionId,
        ResolveContradictionRequest request,
        CancellationToken cancellationToken);
}
