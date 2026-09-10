using System.Text.Json;
using ReqLens.Eval;

var verifyGold = args.Contains("--verify-gold");
var listOnly = args.Contains("--list");
var api = ReadOption(args, "--api") ?? "http://localhost:5080";
var delayMs = int.TryParse(ReadOption(args, "--delay-ms"), out var parsedDelay) ? parsedDelay : 3000;
var outPath = ReadOption(args, "--out");
var stepDelay = TimeSpan.FromMilliseconds(Math.Max(0, delayMs));

string repoRoot;
try
{
    repoRoot = CaseLoader.FindRepoRoot(Directory.GetCurrentDirectory());
}
catch (InvalidOperationException)
{
    repoRoot = CaseLoader.FindRepoRoot(AppContext.BaseDirectory);
}

var casesDir = ReadOption(args, "--cases") ?? Path.Combine(repoRoot, "eval", "cases");
var cases = CaseLoader.Load(casesDir);
var goldErrors = GoldVerifier.Verify(cases);
if (goldErrors.Count > 0)
{
    foreach (var error in goldErrors)
    {
        Console.Error.WriteLine(error);
    }

    return 2;
}

if (listOnly || verifyGold)
{
    Console.WriteLine($"Gold files OK ({cases.Count} cases) in {casesDir}");
    foreach (var item in cases)
    {
        Console.WriteLine();
        Console.WriteLine($"{item.Gold.Id} — {item.Gold.Title}");
        Console.WriteLine(item.Gold.WhyThisCase);
        Console.WriteLine(
            $"  extract {item.Gold.MustExtract.Count}, ambiguities {item.Gold.Ambiguities.Count}, contradictions {item.Gold.Contradictions.Count}, actor {item.Gold.ActorChecks.Count}");
    }

    return 0;
}

Console.WriteLine($"Running {cases.Count} cases against {api}");
Console.WriteLine($"Pacing: {delayMs}ms between pipeline steps and after each case (Groq free-tier TPM).");
var client = new PipelineApi(api);
var results = new List<CaseResult>();
for (var i = 0; i < cases.Count; i++)
{
    var item = cases[i];
    Console.WriteLine();
    Console.WriteLine($"→ {item.Gold.Id} — {item.Gold.Title}");
    var result = await EvalRunner.RunAsync(item, client, stepDelay, CancellationToken.None);
    results.Add(result);
    PrintCase(result);
    if (delayMs > 0 && i < cases.Count - 1)
    {
        await Task.Delay(delayMs);
    }
}

var extractHit = Count(results, result => result.Extraction);
var ambHit = Count(results, result => result.Ambiguities);
var contradictionHit = Count(results, result => result.Contradictions);
var completed = results.Where(result => result.Error is null).ToList();
var actorFail = completed.SelectMany(result => result.Actor).Count(item => item.Kind == "actor" && !item.Hit);
var mustNotFail = completed.SelectMany(result => result.MustNotDetect).Count(item => !item.Hit);
var errors = results.Count(result => result.Error is not null);
var rateLimited = results.Any(result => IsRateLimit(result.Error));

Console.WriteLine();
Console.WriteLine("======== Summary ========");
Console.WriteLine($"Extraction obligations recovered: {extractHit.Hit} of {extractHit.Total}");
Console.WriteLine($"Planted ambiguities caught:      {ambHit.Hit} of {ambHit.Total}");
Console.WriteLine($"Planted contradictions caught:    {contradictionHit.Hit} of {contradictionHit.Total}");
Console.WriteLine($"Forbidden flags (actor-undefined on 'the system', etc.): {mustNotFail} failed checks");
Console.WriteLine($"Actor regex regressions (not including documented tradeoffs): {actorFail}");
Console.WriteLine($"Cases that errored: {errors}");
if (errors > 0 || rateLimited)
{
    Console.WriteLine();
    Console.WriteLine("DO NOT QUOTE these rates. One or more cases aborted (often Groq 429 after retries).");
    Console.WriteLine("A miss from an aborted case is not a detection miss — re-run after the rate limit recovers.");
}
else if (ambHit.Total > 0)
{
    Console.WriteLine();
    Console.WriteLine($"Portfolio line: caught {ambHit.Hit} of {ambHit.Total} planted ambiguities.");
}

var report = new EvalReport
{
    Api = api,
    CasesDirectory = casesDir,
    GeneratedAt = DateTimeOffset.UtcNow,
    DelayMs = delayMs,
    Extraction = extractHit,
    Ambiguities = ambHit,
    Contradictions = contradictionHit,
    ActorRegressions = actorFail,
    ForbiddenFlagFailures = mustNotFail,
    CaseErrors = errors,
    RateLimited = rateLimited,
    Quoteable = errors == 0 && !rateLimited,
    Cases = results
};

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
var dest = outPath ?? Path.Combine(repoRoot, "eval", "last-report.json");
Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
File.WriteAllText(dest, json);
Console.WriteLine($"Wrote {dest}");

return errors > 0 || goldErrors.Count > 0 ? 1 : 0;

static bool IsRateLimit(string? error) =>
    error is not null
    && (error.Contains("429", StringComparison.Ordinal)
        || error.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
        || error.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase));

static void PrintCase(CaseResult result)
{
    if (result.Error is not null)
    {
        Console.WriteLine($"  ERROR {result.Error}");
    }

    WriteChecks("extract", result.Extraction);
    WriteChecks("ambiguity", result.Ambiguities);
    WriteChecks("contradiction", result.Contradictions);
    WriteChecks("must-not", result.MustNotDetect);
    WriteChecks("actor", result.Actor);
}

static void WriteChecks(string label, IReadOnlyList<CheckResult> checks)
{
    foreach (var check in checks)
    {
        var mark = check.Kind == "actor-tradeoff" ? "TRADEOFF" : check.Hit ? "HIT " : "MISS";
        Console.WriteLine($"  [{mark}] {label} {check.Id}: {check.Detail}");
    }
}

static CheckCount Count(IReadOnlyList<CaseResult> results, Func<CaseResult, IReadOnlyList<CheckResult>> selector)
{
    var items = results.SelectMany(selector).ToList();
    return new CheckCount(items.Count(item => item.Hit), items.Count);
}

static string? ReadOption(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    if (index < 0 || index + 1 >= args.Length)
    {
        return null;
    }

    return args[index + 1];
}
