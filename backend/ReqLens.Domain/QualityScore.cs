namespace ReqLens.Domain;

public sealed class QualityScore
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int Overall { get; set; }
    public int Completeness { get; set; }
    public int Clarity { get; set; }
    public int Testability { get; set; }
    public int Consistency { get; set; }
    public int Specificity { get; set; }
    public int RequirementCount { get; set; }
    public int VagueCount { get; set; }
    public int VagueScore { get; set; }
    public int HasActorCount { get; set; }
    public int ActorScore { get; set; }
    public int HasMeasurableConstraintCount { get; set; }
    public int MeasurableConstraintCoverage { get; set; }
    public int UnresolvedContradictionCount { get; set; }
    public int InvolvedInContradictionCount { get; set; }
    public int UnansweredQuestionCount { get; set; }
    public int QuestionScore { get; set; }
    public int LlmClarity { get; set; }
    public int LlmSpecificity { get; set; }
    public Document Document { get; set; } = null!;
}
