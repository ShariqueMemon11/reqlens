namespace ReqLens.Application.Documents;

/// <summary>
/// Deterministic clarification suffix applied when an ambiguity is resolved.
/// Must stay in sync with QualityScoreCalculator's concrete-operation list so
/// chips like Create/Edit/Deactivate clear a vague-verb flag on re-score.
/// </summary>
public static class RequirementClarification
{
    public static string Append(string text, IReadOnlyList<string> selectedOptions)
    {
        var labels = string.Join(", ", selectedOptions.Select(option => option.Trim()).Where(option => option.Length > 0));
        var trimmed = text.TrimEnd();
        return $"{trimmed} (Clarified: {labels}.)";
    }
}
