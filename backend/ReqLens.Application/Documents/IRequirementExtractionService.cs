namespace ReqLens.Application.Documents;

public sealed record ExtractedRequirementDto(string Id, string Text, int SourceLine);

public sealed record ExtractRequirementsResult(
    Guid DocumentId,
    IReadOnlyList<ExtractedRequirementDto> Requirements);

public interface IRequirementExtractionService
{
    Task<ExtractRequirementsResult> ExtractAsync(Guid documentId, CancellationToken cancellationToken);
}
