using ReqLens.Application.Documents;

namespace ReqLens.Eval;

public sealed class CaseResult
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public List<CheckResult> Extraction { get; init; } = [];
    public List<CheckResult> Ambiguities { get; init; } = [];
    public List<CheckResult> Contradictions { get; init; } = [];
    public List<CheckResult> MustNotDetect { get; init; } = [];
    public List<CheckResult> Actor { get; init; } = [];
    public string? Error { get; init; }
}

public sealed class CheckResult
{
    public string Id { get; init; } = "";
    public bool Hit { get; init; }
    public string Kind { get; init; } = "";
    public string Detail { get; init; } = "";
}

public static class EvalRunner
{
    public static async Task<CaseResult> RunAsync(
        EvalCase item,
        PipelineApi api,
        TimeSpan stepDelay,
        CancellationToken cancellationToken)
    {
        try
        {
            var upload = await api.UploadTextAsync(item.DocumentText, cancellationToken);
            var extract = await api.ExtractAsync(upload.Id, cancellationToken);
            var texts = extract.Requirements.ToDictionary(
                requirement => requirement.Id,
                requirement => requirement.Text,
                StringComparer.OrdinalIgnoreCase);
            var allTexts = extract.Requirements.Select(requirement => requirement.Text).ToList();

            var extraction = item.Gold.MustExtract
                .Select(plant => Hit(
                    plant.Id,
                    allTexts.Any(text => Contains(text, plant.Text)),
                    "extraction",
                    allTexts.Any(text => Contains(text, plant.Text))
                        ? "found in extracted text"
                        : "not present in any extracted unit"))
                .ToList();

            await PauseAsync(stepDelay, cancellationToken);
            var ambiguities = await api.DetectAmbiguitiesAsync(upload.Id, cancellationToken);
            await PauseAsync(stepDelay, cancellationToken);
            var contradiction = await api.DetectContradictionsAsync(upload.Id, cancellationToken);

            var ambiguityHits = item.Gold.Ambiguities
                .Select(plant =>
                {
                    var hit = ambiguities.Ambiguities.Any(ambiguity =>
                        texts.TryGetValue(ambiguity.RequirementId, out var text) && Contains(text, plant.Text));
                    return Hit(
                        plant.Id,
                        hit,
                        "ambiguity",
                        hit ? "requirement text matched a detected issue" : "no detected issue on a matching requirement");
                })
                .ToList();

            var contradictionHits = item.Gold.Contradictions
                .Select(plant =>
                {
                    var hit = contradiction.Contradictions.Any(pair =>
                    {
                        var sides = pair.RequirementIds
                            .Select(id => texts.GetValueOrDefault(id, ""))
                            .ToList();
                        return PairMatches(sides, plant.NeedleA, plant.NeedleB);
                    });
                    return Hit(
                        plant.Id,
                        hit,
                        "contradiction",
                        hit ? "detected pair covers both needles" : "no detected pair covers both needles");
                })
                .ToList();

            var forbidden = item.Gold.MustNotDetect
                .Select(plant =>
                {
                    var flagged = ambiguities.Ambiguities.Where(ambiguity =>
                        texts.TryGetValue(ambiguity.RequirementId, out var text)
                        && Contains(text, plant.RequirementNeedle)
                        && plant.IssueNeedles.Any(issueNeedle => Contains(ambiguity.Issue, issueNeedle)))
                        .ToList();
                    var hit = flagged.Count == 0;
                    return Hit(
                        plant.Id,
                        hit,
                        "must-not-detect",
                        hit
                            ? "no forbidden issue text on this requirement"
                            : $"flagged: {string.Join("; ", flagged.Select(item => item.Issue))}");
                })
                .ToList();

            var actor = item.Gold.ActorChecks
                .Select(plant => ScoreActor(plant, allTexts))
                .ToList();

            return new CaseResult
            {
                Id = item.Gold.Id,
                Title = item.Gold.Title,
                Extraction = extraction,
                Ambiguities = ambiguityHits,
                Contradictions = contradictionHits,
                MustNotDetect = forbidden,
                Actor = actor
            };
        }
        catch (Exception ex)
        {
            return Aborted(item, ex.Message);
        }
    }

    private static CaseResult Aborted(EvalCase item, string error)
    {
        string Detail(string prefix) => $"{prefix}: {error}";
        return new CaseResult
        {
            Id = item.Gold.Id,
            Title = item.Gold.Title,
            Extraction = item.Gold.MustExtract
                .Select(plant => Hit(plant.Id, false, "extraction", Detail("case aborted before this check")))
                .ToList(),
            Ambiguities = item.Gold.Ambiguities
                .Select(plant => Hit(plant.Id, false, "ambiguity", Detail("case aborted before this check")))
                .ToList(),
            Contradictions = item.Gold.Contradictions
                .Select(plant => Hit(plant.Id, false, "contradiction", Detail("case aborted before this check")))
                .ToList(),
            MustNotDetect = item.Gold.MustNotDetect
                .Select(plant => Hit(plant.Id, false, "must-not-detect", Detail("case aborted before this check")))
                .ToList(),
            Actor = item.Gold.ActorChecks
                .Select(plant => Hit(
                    plant.Id,
                    false,
                    plant.DocumentedTradeoff ? "actor-tradeoff" : "actor",
                    Detail("case aborted before this check")))
                .ToList(),
            Error = error
        };
    }

    private static Task PauseAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        delay > TimeSpan.Zero ? Task.Delay(delay, cancellationToken) : Task.CompletedTask;

    private static CheckResult ScoreActor(ActorCheck plant, IReadOnlyList<string> texts)
    {
        var match = texts.FirstOrDefault(text => Contains(text, plant.Needle));
        if (match is null)
        {
            return Hit(plant.Id, false, plant.DocumentedTradeoff ? "actor-tradeoff" : "actor", "requirement not extracted, cannot score actor");
        }

        var actual = QualityScoreCalculator.HasActor(match);
        if (plant.DocumentedTradeoff)
        {
            return new CheckResult
            {
                Id = plant.Id,
                Hit = true,
                Kind = "actor-tradeoff",
                Detail = actual
                    ? "HasActor=true (unexpected vs the documented tradeoff — scoring may have changed)"
                    : "HasActor=false, as documented for prohibition/constraint sentences with no grammatical actor. Not counted as a detection miss."
            };
        }

        return Hit(
            plant.Id,
            actual == plant.ExpectHasActor,
            "actor",
            $"HasActor={actual}, expected {plant.ExpectHasActor}");
    }

    private static CheckResult Hit(string id, bool hit, string kind, string detail) =>
        new() { Id = id, Hit = hit, Kind = kind, Detail = detail };

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static bool PairMatches(IReadOnlyList<string> sides, string needleA, string needleB)
    {
        if (sides.Count < 2)
        {
            return false;
        }

        var left = sides[0];
        var right = sides[1];
        return (Contains(left, needleA) && Contains(right, needleB))
            || (Contains(left, needleB) && Contains(right, needleA));
    }
}
