namespace ReqLens.Application.Documents;

public sealed class ContradictionDetectionOptions
{
    public const string SectionName = "ContradictionDetection";

    /// <summary>
    /// Optional cosine floor. A neighbor below this is never a candidate, even if it is in the top-K.
    /// Gemini embeddings in one domain often sit well above 0.70, so this is not the main filter —
    /// NeighborsPerRequirement is.
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.70;

    /// <summary>
    /// Each requirement is compared only to this many nearest other requirements (after the floor
    /// and skipping same-source-line splits). Unique undirected pairs are unioned and all of them
    /// are judged — none are dropped for being outside a global top-N.
    /// </summary>
    public int NeighborsPerRequirement { get; set; } = 5;

    /// <summary>
    /// Max pairs in a single LLM judgment call. Extra neighbor-pairs are split into further calls,
    /// not discarded.
    /// </summary>
    public int MaxCandidatePairs { get; set; } = 15;
}
