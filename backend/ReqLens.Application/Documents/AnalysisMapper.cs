using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public static class AnalysisMapper
{
    public static ExtractedRequirementDto ToRequirementDto(Requirement requirement) =>
        new(requirement.RequirementId, requirement.Text, requirement.SourceLine);

    public static AmbiguityDto ToAmbiguityDto(Ambiguity ambiguity)
    {
        var selected = string.IsNullOrWhiteSpace(ambiguity.ResolvedSelections)
            ? Array.Empty<string>()
            : ambiguity.ResolvedSelections.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new AmbiguityDto(
            ambiguity.Id,
            ambiguity.RequirementId,
            ambiguity.Severity.ToString().ToLowerInvariant(),
            ambiguity.Issue,
            ambiguity.Questions
                .OrderBy(question => question.SortOrder)
                .Select(question => new AmbiguityQuestionDto(question.Text, question.OptionType))
                .ToList(),
            ambiguity.IsResolved,
            selected);
    }

    public static ContradictionDto ToContradictionDto(Contradiction contradiction) =>
        new(
            contradiction.Id,
            [contradiction.RequirementIdA, contradiction.RequirementIdB],
            contradiction.Description,
            contradiction.ResolutionQuestion,
            contradiction.IsResolved,
            string.IsNullOrWhiteSpace(contradiction.ChosenRequirementId)
                ? null
                : contradiction.ChosenRequirementId);
}
