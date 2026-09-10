using ReqLens.Application.Documents;

namespace ReqLens.Eval;

public static class GoldVerifier
{
    public static IReadOnlyList<string> Verify(IReadOnlyList<EvalCase> cases)
    {
        var errors = new List<string>();
        foreach (var item in cases)
        {
            var doc = item.DocumentText;
            foreach (var plant in item.Gold.MustExtract)
            {
                CheckNeedle(errors, item.Stem, "mustExtract", plant.Id, plant.Text, doc);
            }

            foreach (var plant in item.Gold.Ambiguities)
            {
                CheckNeedle(errors, item.Stem, "ambiguities", plant.Id, plant.Text, doc);
            }

            foreach (var plant in item.Gold.Contradictions)
            {
                CheckNeedle(errors, item.Stem, "contradictions.needleA", plant.Id, plant.NeedleA, doc);
                CheckNeedle(errors, item.Stem, "contradictions.needleB", plant.Id, plant.NeedleB, doc);
            }

            foreach (var plant in item.Gold.MustNotDetect)
            {
                CheckNeedle(errors, item.Stem, "mustNotDetect", plant.Id, plant.RequirementNeedle, doc);
            }

            foreach (var plant in item.Gold.ActorChecks)
            {
                CheckNeedle(errors, item.Stem, "actorChecks", plant.Id, plant.Needle, doc);
                if (Contains(doc, plant.Needle))
                {
                    var actual = QualityScoreCalculator.HasActor(SentenceContaining(doc, plant.Needle));
                    if (actual != plant.ExpectHasActor)
                    {
                        errors.Add(
                            $"{item.Stem} actorChecks.{plant.Id}: source sentence HasActor={actual}, gold expectHasActor={plant.ExpectHasActor}. Fix the sentence or the gold.");
                    }
                }
            }
        }

        return errors;
    }

    private static void CheckNeedle(
        List<string> errors,
        string stem,
        string field,
        string id,
        string needle,
        string document)
    {
        if (string.IsNullOrWhiteSpace(needle))
        {
            errors.Add($"{stem} {field}.{id}: empty needle.");
            return;
        }

        if (!Contains(document, needle))
        {
            errors.Add($"{stem} {field}.{id}: needle '{needle}' not found in the test document.");
        }
    }

    private static bool Contains(string document, string needle) =>
        document.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static string SentenceContaining(string document, string needle)
    {
        foreach (var line in document.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim();
            }
        }

        return document;
    }
}
