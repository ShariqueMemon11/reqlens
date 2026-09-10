namespace ReqLens.Application.Documents;

public sealed record ContradictionDto(
    Guid Id,
    IReadOnlyList<string> RequirementIds,
    string Description,
    string ResolutionQuestion,
    bool IsResolved,
    string? ChosenRequirementId);

public sealed record DetectContradictionsResult(
    Guid DocumentId,
    IReadOnlyList<ContradictionDto> Contradictions,
    bool CandidateCapReached,
    int CandidatePairsAboveThreshold,
    int CandidatePairsSentToLlm,
    double SimilarityThreshold,
    int NeighborsPerRequirement);

public interface IContradictionDetectionService
{
    Task<DetectContradictionsResult> DetectAsync(Guid documentId, CancellationToken cancellationToken);
}
