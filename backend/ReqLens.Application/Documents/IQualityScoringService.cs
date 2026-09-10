namespace ReqLens.Application.Documents;

public sealed record QualitySignalsDto(
    int RequirementCount,
    int VagueCount,
    int VagueScore,
    int HasActorCount,
    int ActorScore,
    int HasMeasurableConstraintCount,
    int MeasurableConstraintCoverage,
    int UnresolvedContradictionCount,
    int InvolvedInContradictionCount,
    int UnansweredQuestionCount,
    int QuestionScore,
    int LlmClarity,
    int LlmSpecificity);

public sealed record ComputeQualityScoreResult(
    Guid DocumentId,
    int Overall,
    int Completeness,
    int Clarity,
    int Testability,
    int Consistency,
    int Specificity,
    QualitySignalsDto Signals);

public interface IQualityScoringService
{
    Task<ComputeQualityScoreResult> ScoreAsync(Guid documentId, CancellationToken cancellationToken);
    Task<ComputeQualityScoreResult> ScoreAsync(
        Guid documentId,
        bool reuseStoredLlmScores,
        CancellationToken cancellationToken);
}
