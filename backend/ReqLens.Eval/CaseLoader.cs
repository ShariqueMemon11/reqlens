using System.Text.Json;

namespace ReqLens.Eval;

public static class CaseLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ReqLens_V1_Spec.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "eval", "cases")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not find the repo root (looking for docs/ReqLens_V1_Spec.md and eval/cases). Run from the reqlens folder.");
    }

    public static IReadOnlyList<EvalCase> Load(string casesDirectory)
    {
        if (!Directory.Exists(casesDirectory))
        {
            throw new InvalidOperationException($"Cases folder not found: {casesDirectory}");
        }

        var goldFiles = Directory.GetFiles(casesDirectory, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
        if (goldFiles.Count == 0)
        {
            throw new InvalidOperationException($"No *.json gold files in {casesDirectory}");
        }

        var cases = new List<EvalCase>();
        foreach (var goldPath in goldFiles)
        {
            var stem = Path.GetFileNameWithoutExtension(goldPath);
            var docPath = Path.Combine(casesDirectory, stem + ".txt");
            if (!File.Exists(docPath))
            {
                throw new InvalidOperationException($"Gold {goldPath} has no matching {stem}.txt");
            }

            var gold = JsonSerializer.Deserialize<GoldFile>(File.ReadAllText(goldPath), JsonOptions)
                ?? throw new InvalidOperationException($"Could not parse {goldPath}");
            if (string.IsNullOrWhiteSpace(gold.Id))
            {
                gold.Id = stem;
            }

            cases.Add(new EvalCase
            {
                Stem = stem,
                DocumentPath = docPath,
                GoldPath = goldPath,
                DocumentText = File.ReadAllText(docPath),
                Gold = gold
            });
        }

        return cases;
    }
}
