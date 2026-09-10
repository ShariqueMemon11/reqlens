using ReqLens.Eval;

namespace ReqLens.Tests;

public sealed class EvalGoldFilesTests
{
    [Fact]
    public void GoldNeedles_AppearInMatchingDocuments_AndActorExpectationsMatchSource()
    {
        var root = CaseLoader.FindRepoRoot(AppContext.BaseDirectory);
        var cases = CaseLoader.Load(Path.Combine(root, "eval", "cases"));
        Assert.True(cases.Count >= 12, $"Expected at least 12 eval cases, found {cases.Count}.");
        Assert.Empty(GoldVerifier.Verify(cases));
    }
}
