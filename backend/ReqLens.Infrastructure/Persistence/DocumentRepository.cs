using Microsoft.EntityFrameworkCore;
using ReqLens.Application.Documents;
using ReqLens.Domain;
using ReqLens.Infrastructure.Persistence;

namespace ReqLens.Infrastructure.Persistence;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;

    public DocumentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Documents.FirstOrDefaultAsync(document => document.Id == id, cancellationToken);

    public async Task ReplaceRequirementsAsync(
        Document document,
        IReadOnlyList<Requirement> requirements,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Requirements
            .Where(requirement => requirement.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        _db.Requirements.RemoveRange(existing);
        _db.Requirements.AddRange(requirements);

        var staleAmbiguities = await _db.Ambiguities
            .Include(ambiguity => ambiguity.Questions)
            .Where(ambiguity => ambiguity.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        _db.Ambiguities.RemoveRange(staleAmbiguities);

        var staleContradictions = await _db.Contradictions
            .Where(contradiction => contradiction.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        _db.Contradictions.RemoveRange(staleContradictions);

        var staleScores = await _db.QualityScores
            .Where(score => score.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        _db.QualityScores.RemoveRange(staleScores);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Requirement>> GetRequirementsAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _db.Requirements
            .Where(requirement => requirement.DocumentId == documentId)
            .OrderBy(requirement => requirement.RequirementId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceAmbiguitiesAsync(
        Guid documentId,
        IReadOnlyList<Ambiguity> ambiguities,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Ambiguities
            .Include(ambiguity => ambiguity.Questions)
            .Where(ambiguity => ambiguity.DocumentId == documentId)
            .ToListAsync(cancellationToken);
        _db.Ambiguities.RemoveRange(existing);
        _db.Ambiguities.AddRange(ambiguities);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceContradictionsAsync(
        Guid documentId,
        IReadOnlyList<Contradiction> contradictions,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Contradictions
            .Where(contradiction => contradiction.DocumentId == documentId)
            .ToListAsync(cancellationToken);
        _db.Contradictions.RemoveRange(existing);
        _db.Contradictions.AddRange(contradictions);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ambiguity>> GetAmbiguitiesAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _db.Ambiguities
            .Include(ambiguity => ambiguity.Questions)
            .Where(ambiguity => ambiguity.DocumentId == documentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contradiction>> GetContradictionsAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _db.Contradictions
            .Where(contradiction => contradiction.DocumentId == documentId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceQualityScoreAsync(
        Guid documentId,
        QualityScore score,
        CancellationToken cancellationToken)
    {
        var existing = await _db.QualityScores
            .Where(item => item.DocumentId == documentId)
            .ToListAsync(cancellationToken);
        _db.QualityScores.RemoveRange(existing);
        _db.QualityScores.Add(score);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<QualityScore?> GetQualityScoreAsync(
        Guid documentId,
        CancellationToken cancellationToken) =>
        _db.QualityScores.FirstOrDefaultAsync(score => score.DocumentId == documentId, cancellationToken);

    public Task<Ambiguity?> GetAmbiguityAsync(
        Guid documentId,
        Guid ambiguityId,
        CancellationToken cancellationToken) =>
        _db.Ambiguities
            .Include(ambiguity => ambiguity.Questions)
            .FirstOrDefaultAsync(
                ambiguity => ambiguity.DocumentId == documentId && ambiguity.Id == ambiguityId,
                cancellationToken);

    public Task<Contradiction?> GetContradictionAsync(
        Guid documentId,
        Guid contradictionId,
        CancellationToken cancellationToken) =>
        _db.Contradictions.FirstOrDefaultAsync(
            contradiction => contradiction.DocumentId == documentId && contradiction.Id == contradictionId,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
