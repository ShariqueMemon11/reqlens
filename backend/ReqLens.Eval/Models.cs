using System.Text.Json.Serialization;

namespace ReqLens.Eval;

public sealed class GoldFile
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string WhyThisCase { get; set; } = "";
    public List<Needle> MustExtract { get; set; } = [];
    public List<Needle> Ambiguities { get; set; } = [];
    public List<ContradictionPlant> Contradictions { get; set; } = [];
    public List<MustNotDetect> MustNotDetect { get; set; } = [];
    public List<ActorCheck> ActorChecks { get; set; } = [];
}

public sealed class Needle
{
    public string Id { get; set; } = "";
    [JsonPropertyName("needle")]
    public string Text { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class ContradictionPlant
{
    public string Id { get; set; } = "";
    public string NeedleA { get; set; } = "";
    public string NeedleB { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class MustNotDetect
{
    public string Id { get; set; } = "";
    public string RequirementNeedle { get; set; } = "";
    public List<string> IssueNeedles { get; set; } = [];
    public string Note { get; set; } = "";
}

public sealed class ActorCheck
{
    public string Id { get; set; } = "";
    public string Needle { get; set; } = "";
    public bool ExpectHasActor { get; set; }
    public bool DocumentedTradeoff { get; set; }
    public string Note { get; set; } = "";
}

public sealed class EvalCase
{
    public required string Stem { get; init; }
    public required string DocumentPath { get; init; }
    public required string GoldPath { get; init; }
    public required string DocumentText { get; init; }
    public required GoldFile Gold { get; init; }
}

public sealed record CheckCount(int Hit, int Total);

public sealed class EvalReport
{
    public string Api { get; init; } = "";
    public string CasesDirectory { get; init; } = "";
    public DateTimeOffset GeneratedAt { get; init; }
    public int DelayMs { get; init; }
    public CheckCount Extraction { get; init; } = new(0, 0);
    public CheckCount Ambiguities { get; init; } = new(0, 0);
    public CheckCount Contradictions { get; init; } = new(0, 0);
    public int ActorRegressions { get; init; }
    public int ForbiddenFlagFailures { get; init; }
    public int CaseErrors { get; init; }
    public bool RateLimited { get; init; }
    public bool Quoteable { get; init; }
    public List<CaseResult> Cases { get; init; } = [];
}
