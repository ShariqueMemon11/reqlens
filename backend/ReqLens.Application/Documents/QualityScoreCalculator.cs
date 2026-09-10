using System.Text.RegularExpressions;

namespace ReqLens.Application.Documents;

/// <summary>
/// Deterministic, interview-defensible quality signals. No LLM.
/// measurableConstraintCoverage is a text-heuristic proxy — V1 does not extract real acceptance criteria.
/// </summary>
public static class QualityScoreCalculator
{
    private static readonly Regex VagueVerb = new(
        @"\b(?:manag(?:e|es|ed|ing)|handl(?:e|es|ed|ing)|support(?:s|ed|ing)?|process(?:es|ed|ing)?|maintain(?:s|ed|ing)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ConcreteOperation = new(
        @"\b(create|edit|update|delete|view|assign|reassign|revoke|upload|export|cancel|approve|lock|notify|deactivate|reset|generate|archive|download|respond|close|login|logout)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HumanActorSubject = new(
        @"\b(?:support[ -]agents?|admins?|administrators?|managers?|users?|customers?|agents?|operators?|employees?)\s+(can|shall|must|should|may|will)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SystemActor = new(
        @"\bthe system\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NumberWithUnit = new(
        @"\b\d+(?:\.\d+)?\s*(?:seconds?|minutes?|hours?|days?|characters?|attempts?|dollars?|bytes?|kb|mb|gb)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BoundWithNumber = new(
        @"\b(?:at least|at most|within|after|before)\s+\d+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DollarAmount = new(
        @"\$\s*\d+(?:\.\d+)?",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex GivenWhenThen = new(
        @"\bgiven\b.*\bwhen\b.*\bthen\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool HasVagueVerbWithoutOperations(string text)
    {
        if (!VagueVerb.IsMatch(text))
        {
            return false;
        }

        return CountConcreteOperations(text) < 2;
    }

    /// <summary>
    /// True when a human role is the grammatical subject of a permission verb, or "the system" appears.
    /// V1 limitation (accepted): prohibition/constraint sentences with no actor by nature score as
    /// missing-actor — e.g. "Shipped orders cannot be cancelled.", "The reports should be managed properly."
    /// Phase 4 does not FLAG those as undefined-actor (same carve-out idea as treating "the system" as
    /// valid for automated behavior). Conservative here is an explainable completeness tradeoff, not a bug.
    /// </summary>
    public static bool HasActor(string text) =>
        HumanActorSubject.IsMatch(text) || SystemActor.IsMatch(text);

    public static bool HasMeasurableConstraint(string text) =>
        NumberWithUnit.IsMatch(text)
        || BoundWithNumber.IsMatch(text)
        || DollarAmount.IsMatch(text)
        || GivenWhenThen.IsMatch(text);

    public static QualityRuleSignals ComputeSignals(
        IReadOnlyList<string> requirementIds,
        IReadOnlyList<string> requirementTexts,
        IReadOnlyList<(string IdA, string IdB)> contradictions,
        int unansweredQuestionCount)
    {
        var n = requirementTexts.Count;
        if (n == 0)
        {
            return QualityRuleSignals.Empty;
        }

        var vagueCount = requirementTexts.Count(HasVagueVerbWithoutOperations);
        var hasActorCount = requirementTexts.Count(HasActor);
        var hasConstraintCount = requirementTexts.Count(HasMeasurableConstraint);

        var involved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (idA, idB) in contradictions)
        {
            involved.Add(idA);
            involved.Add(idB);
        }

        var involvedCount = requirementIds
            .Count(id => involved.Contains(id));

        var questionDenom = Math.Max(1, 3 * n);
        var questionRatio = Math.Min(1.0, unansweredQuestionCount / (double)questionDenom);

        return new QualityRuleSignals(
            RequirementCount: n,
            VagueCount: vagueCount,
            VagueScore: ToScore(100 * (1 - vagueCount / (double)n)),
            HasActorCount: hasActorCount,
            ActorScore: ToScore(100 * (hasActorCount / (double)n)),
            HasMeasurableConstraintCount: hasConstraintCount,
            MeasurableConstraintCoverage: ToScore(100 * (hasConstraintCount / (double)n)),
            UnresolvedContradictionCount: contradictions.Count,
            InvolvedInContradictionCount: involvedCount,
            Consistency: ToScore(100 * (1 - involvedCount / (double)n)),
            UnansweredQuestionCount: unansweredQuestionCount,
            QuestionScore: ToScore(100 * (1 - questionRatio)));
    }

    public static QualityBars Combine(QualityRuleSignals signals, int llmClarity, int llmSpecificity)
    {
        if (signals.RequirementCount == 0)
        {
            return QualityBars.Empty;
        }

        var completeness = ToScore(0.5 * signals.ActorScore + 0.5 * signals.MeasurableConstraintCoverage);
        var testability = ToScore(0.6 * signals.MeasurableConstraintCoverage + 0.4 * signals.QuestionScore);
        var clarity = ToScore(llmClarity);
        var specificity = ToScore(0.5 * signals.VagueScore + 0.5 * llmSpecificity);
        var overall = ToScore(
            (completeness + clarity + testability + signals.Consistency + specificity) / 5.0);

        return new QualityBars(
            Overall: overall,
            Completeness: completeness,
            Clarity: clarity,
            Testability: testability,
            Consistency: signals.Consistency,
            Specificity: specificity,
            LlmClarity: clarity,
            LlmSpecificity: ToScore(llmSpecificity));
    }

    private static int CountConcreteOperations(string text)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in ConcreteOperation.Matches(text))
        {
            seen.Add(match.Value);
        }

        return seen.Count;
    }

    internal static int ToScore(double value) =>
        Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 100);
}

public sealed record QualityRuleSignals(
    int RequirementCount,
    int VagueCount,
    int VagueScore,
    int HasActorCount,
    int ActorScore,
    int HasMeasurableConstraintCount,
    int MeasurableConstraintCoverage,
    int UnresolvedContradictionCount,
    int InvolvedInContradictionCount,
    int Consistency,
    int UnansweredQuestionCount,
    int QuestionScore)
{
    public static QualityRuleSignals Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record QualityBars(
    int Overall,
    int Completeness,
    int Clarity,
    int Testability,
    int Consistency,
    int Specificity,
    int LlmClarity,
    int LlmSpecificity)
{
    public static QualityBars Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);
}
