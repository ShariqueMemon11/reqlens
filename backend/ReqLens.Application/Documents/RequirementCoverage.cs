using System.Text.RegularExpressions;

namespace ReqLens.Application.Documents;

internal static class RequirementCoverage
{
    private static readonly Regex ClauseSplit = new(
        @"(?:,\s+and\s+|;\s+|,\s+with\s+|\s+with\s+|\s+and\s+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public sealed record SourceLine(int Line, string Text, int ExpectedClauses);

    public sealed record LineGap(SourceLine Source, int Produced);

    public static IReadOnlyList<SourceLine> Inventory(string documentText)
    {
        var results = new List<SourceLine>();
        var lines = documentText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var text = lines[i].Trim();
            if (text.Length == 0 || IsHeading(text))
            {
                continue;
            }

            results.Add(new SourceLine(i + 1, text, CountClauses(text)));
        }

        return results;
    }

    public static string NumberedDocument(string documentText)
    {
        var lines = Inventory(documentText);
        return string.Join("\n", lines.Select(line => $"[{line.Line}] {line.Text}"));
    }

    public static IReadOnlyList<LineGap> MissingLines(
        IReadOnlyList<SourceLine> source,
        IEnumerable<int> reportedSourceLines)
    {
        var produced = CountByLine(reportedSourceLines);
        return source
            .Select(line => new LineGap(line, produced.GetValueOrDefault(line.Line)))
            .Where(gap => gap.Produced == 0)
            .ToList();
    }

    public static IReadOnlyList<LineGap> UnderCoveredLines(
        IReadOnlyList<SourceLine> source,
        IEnumerable<int> reportedSourceLines)
    {
        var produced = CountByLine(reportedSourceLines);
        return source
            .Select(line => new LineGap(line, produced.GetValueOrDefault(line.Line)))
            .Where(gap => gap.Produced > 0 && gap.Produced < gap.Source.ExpectedClauses)
            .ToList();
    }

    public static string NextRequirementId(IEnumerable<string> existingIds)
    {
        var max = 0;
        foreach (var id in existingIds)
        {
            if (id.Length >= 3
                && id.StartsWith("R-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(id.AsSpan(2), out var n))
            {
                max = Math.Max(max, n);
            }
        }

        return $"R-{(max + 1).ToString("D3")}";
    }

    private static Dictionary<int, int> CountByLine(IEnumerable<int> reportedSourceLines)
    {
        var counts = new Dictionary<int, int>();
        foreach (var line in reportedSourceLines)
        {
            counts[line] = counts.GetValueOrDefault(line) + 1;
        }

        return counts;
    }

    private static int CountClauses(string line)
    {
        var coordinated = ClauseSplit.Split(line)
            .Select(part => part.Trim().TrimEnd(',', ';', '.'))
            .Where(part => part.Length > 0)
            .ToList();
        if (coordinated.Count == 0)
        {
            return 1;
        }

        var total = 0;
        foreach (var part in coordinated)
        {
            total += CountCommaActions(part);
        }

        return Math.Max(1, total);
    }

    private static int CountCommaActions(string part)
    {
        var bits = part.Split(',').Select(bit => bit.Trim()).Where(bit => bit.Length > 0).ToList();
        if (bits.Count <= 1)
        {
            return 1;
        }

        var tailsAreShort = bits.Skip(1).All(bit =>
            bit.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 3);
        return tailsAreShort ? bits.Count : 1;
    }

    private static bool IsHeading(string line) =>
        line.StartsWith('#')
        || (line.Length <= 40 && !line.Contains(' ') && !line.EndsWith('.'));
}
