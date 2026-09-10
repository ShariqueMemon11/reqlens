namespace ReqLens.Application.Documents;

public sealed class IssueResolutionService : IIssueResolutionService
{
    private readonly IDocumentRepository _documents;
    private readonly IQualityScoringService _quality;

    public IssueResolutionService(IDocumentRepository documents, IQualityScoringService quality)
    {
        _documents = documents;
        _quality = quality;
    }

    public async Task<ResolveIssueResult> ResolveAmbiguityAsync(
        Guid documentId,
        Guid ambiguityId,
        ResolveAmbiguityRequest request,
        CancellationToken cancellationToken)
    {
        _ = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Document not found.");

        var ambiguity = await _documents.GetAmbiguityAsync(documentId, ambiguityId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Ambiguity not found.");
        if (ambiguity.IsResolved)
        {
            throw new UploadDocumentException(409, "This ambiguity is already resolved.");
        }

        var allowed = ambiguity.Questions
            .Select(question => question.Text)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = (request?.SelectedOptions ?? [])
            .Select(option => option.Trim())
            .Where(option => option.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (selected.Count == 0)
        {
            throw new UploadDocumentException(400, "Select at least one clarification option.");
        }

        if (selected.Any(option => !allowed.Contains(option)))
        {
            throw new UploadDocumentException(400, "One or more selected options are not valid for this issue.");
        }

        var requirements = await _documents.GetRequirementsAsync(documentId, cancellationToken);
        var requirement = requirements.FirstOrDefault(item =>
            string.Equals(item.RequirementId, ambiguity.RequirementId, StringComparison.OrdinalIgnoreCase))
            ?? throw new UploadDocumentException(404, "Requirement not found for this ambiguity.");

        requirement.Text = RequirementClarification.Append(requirement.Text, selected);
        ambiguity.IsResolved = true;
        ambiguity.ResolvedSelections = string.Join(", ", selected);
        await _documents.SaveChangesAsync(cancellationToken);

        var quality = await _quality.ScoreAsync(documentId, reuseStoredLlmScores: false, cancellationToken);
        return await SnapshotAsync(documentId, quality, cancellationToken);
    }

    public async Task<ResolveIssueResult> ResolveContradictionAsync(
        Guid documentId,
        Guid contradictionId,
        ResolveContradictionRequest request,
        CancellationToken cancellationToken)
    {
        _ = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Document not found.");

        var contradiction = await _documents.GetContradictionAsync(documentId, contradictionId, cancellationToken)
            ?? throw new UploadDocumentException(404, "Contradiction not found.");
        if (contradiction.IsResolved)
        {
            throw new UploadDocumentException(409, "This contradiction is already resolved.");
        }

        var chosen = request?.ChosenRequirementId?.Trim() ?? "";
        var isLeft = string.Equals(chosen, contradiction.RequirementIdA, StringComparison.OrdinalIgnoreCase);
        var isRight = string.Equals(chosen, contradiction.RequirementIdB, StringComparison.OrdinalIgnoreCase);
        if (!isLeft && !isRight)
        {
            throw new UploadDocumentException(
                400,
                "Choose which requirement takes precedence (one of the two ids in this contradiction).");
        }

        contradiction.IsResolved = true;
        contradiction.ChosenRequirementId = isLeft ? contradiction.RequirementIdA : contradiction.RequirementIdB;
        await _documents.SaveChangesAsync(cancellationToken);

        var quality = await _quality.ScoreAsync(documentId, reuseStoredLlmScores: true, cancellationToken);
        return await SnapshotAsync(documentId, quality, cancellationToken);
    }

    private async Task<ResolveIssueResult> SnapshotAsync(
        Guid documentId,
        ComputeQualityScoreResult quality,
        CancellationToken cancellationToken)
    {
        var requirements = await _documents.GetRequirementsAsync(documentId, cancellationToken);
        var ambiguities = await _documents.GetAmbiguitiesAsync(documentId, cancellationToken);
        var contradictions = await _documents.GetContradictionsAsync(documentId, cancellationToken);
        return new ResolveIssueResult(
            documentId,
            requirements.Select(AnalysisMapper.ToRequirementDto).ToList(),
            ambiguities.Select(AnalysisMapper.ToAmbiguityDto).ToList(),
            contradictions.Select(AnalysisMapper.ToContradictionDto).ToList(),
            quality);
    }
}
