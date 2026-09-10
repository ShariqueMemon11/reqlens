using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task ReplaceRequirementsAsync(
        Document document,
        IReadOnlyList<Requirement> requirements,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Requirement>> GetRequirementsAsync(
        Guid documentId,
        CancellationToken cancellationToken);
    Task ReplaceAmbiguitiesAsync(
        Guid documentId,
        IReadOnlyList<Ambiguity> ambiguities,
        CancellationToken cancellationToken);
    Task ReplaceContradictionsAsync(
        Guid documentId,
        IReadOnlyList<Contradiction> contradictions,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Ambiguity>> GetAmbiguitiesAsync(
        Guid documentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Contradiction>> GetContradictionsAsync(
        Guid documentId,
        CancellationToken cancellationToken);
    Task ReplaceQualityScoreAsync(
        Guid documentId,
        QualityScore score,
        CancellationToken cancellationToken);
    Task<QualityScore?> GetQualityScoreAsync(
        Guid documentId,
        CancellationToken cancellationToken);
    Task<Ambiguity?> GetAmbiguityAsync(
        Guid documentId,
        Guid ambiguityId,
        CancellationToken cancellationToken);
    Task<Contradiction?> GetContradictionAsync(
        Guid documentId,
        Guid contradictionId,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
