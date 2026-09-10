using ReqLens.Application.Documents;

namespace ReqLens.Tests;

public sealed class QualityScoreCalculatorTests
{
    [Theory]
    [InlineData("Admin should be able to manage users.", true)]
    [InlineData("The system should support user authentication.", true)]
    [InlineData("Handle payment processing for customer orders.", true)]
    [InlineData("The reports should be managed properly.", true)]
    [InlineData("Managers can create, edit, and delete customer accounts.", false)]
    [InlineData("Administrators can assign, reassign, and revoke user roles.", false)]
    [InlineData("Customers can cancel an order at any time.", false)]
    public void VagueVerb_RequiresMissingOperationList(string text, bool expected)
    {
        Assert.Equal(expected, QualityScoreCalculator.HasVagueVerbWithoutOperations(text));
    }

    [Fact]
    public void VagueVerb_ClarifiedAppend_SeesOperationList()
    {
        const string original = "Admin should be able to manage users.";
        Assert.True(QualityScoreCalculator.HasVagueVerbWithoutOperations(original));

        var clarified = RequirementClarification.Append(original, ["Create", "Edit", "Deactivate"]);
        Assert.Equal("Admin should be able to manage users. (Clarified: Create, Edit, Deactivate.)", clarified);
        Assert.False(QualityScoreCalculator.HasVagueVerbWithoutOperations(clarified));
    }

    [Theory]
    [InlineData("Managers can create customer accounts.", true)]
    [InlineData("Users can upload attachments to a ticket.", true)]
    [InlineData("The support agent can view customer tickets.", true)]
    [InlineData("The system shall lock a user account after 5 failed attempts.", true)]
    [InlineData("The system records each account action in the audit log.", true)]
    [InlineData("Handle payment processing for customer orders.", false)]
    [InlineData("Shipped orders cannot be cancelled.", false)]
    public void Actor_HumanSubjectOrTheSystem(string text, bool expected)
    {
        Assert.Equal(expected, QualityScoreCalculator.HasActor(text));
    }

    [Theory]
    [InlineData("The system shall lock a user account after 5 failed login attempts within 10 minutes.", true)]
    [InlineData("Passwords must be at least 12 characters.", true)]
    [InlineData("Refunds require manager approval when the amount exceeds $500.", true)]
    [InlineData("Users can upload attachments to a ticket.", false)]
    [InlineData("The system should support user authentication.", false)]
    [InlineData("Handle payment processing for customer orders.", false)]
    public void MeasurableConstraint_BoundedVsUnbounded(string text, bool expected)
    {
        Assert.Equal(expected, QualityScoreCalculator.HasMeasurableConstraint(text));
    }

    [Fact]
    public void EmptyDocument_AllZeros()
    {
        var signals = QualityScoreCalculator.ComputeSignals([], [], [], 0);
        var bars = QualityScoreCalculator.Combine(signals, 80, 80);

        Assert.Equal(QualityRuleSignals.Empty, signals);
        Assert.Equal(QualityBars.Empty, bars);
    }

    [Fact]
    public void Consistency_TwoOfTwentyTwoInvolved_Is91()
    {
        var ids = Enumerable.Range(1, 22).Select(i => $"R-{i:D3}").ToList();
        var texts = ids.Select(_ => "Managers can create accounts.").ToList();
        var signals = QualityScoreCalculator.ComputeSignals(
            ids,
            texts,
            [("R-012", "R-013")],
            unansweredQuestionCount: 0);

        Assert.Equal(2, signals.InvolvedInContradictionCount);
        Assert.Equal(1, signals.UnresolvedContradictionCount);
        Assert.Equal(91, signals.Consistency);
    }

    [Fact]
    public void QuestionScore_CapsAtThreePerRequirement()
    {
        var ids = Enumerable.Range(1, 10).Select(i => $"R-{i:D3}").ToList();
        var texts = ids.Select(_ => "Managers can create accounts.").ToList();

        var none = QualityScoreCalculator.ComputeSignals(ids, texts, [], 0);
        var half = QualityScoreCalculator.ComputeSignals(ids, texts, [], 15);
        var full = QualityScoreCalculator.ComputeSignals(ids, texts, [], 30);
        var over = QualityScoreCalculator.ComputeSignals(ids, texts, [], 40);

        Assert.Equal(100, none.QuestionScore);
        Assert.Equal(50, half.QuestionScore);
        Assert.Equal(0, full.QuestionScore);
        Assert.Equal(0, over.QuestionScore);
    }

    [Fact]
    public void Combine_UsesPublishedWeights()
    {
        var ids = Enumerable.Range(1, 4).Select(i => $"R-{i:D3}").ToList();
        var texts = new[]
        {
            "Managers can create accounts.",
            "Handle payment processing.",
            "The system shall lock a user account after 5 failed attempts.",
            "Users can upload attachments to a ticket."
        };
        var signals = QualityScoreCalculator.ComputeSignals(
            ids,
            texts,
            [("R-001", "R-002")],
            unansweredQuestionCount: 6);

        // 1 vague (Handle) → 75; 3 actors → 75; 1 measurable → 25;
        // 2 involved / 4 → consistency 50; questions 6 / 12 → 50.
        Assert.Equal(1, signals.VagueCount);
        Assert.Equal(75, signals.VagueScore);
        Assert.Equal(3, signals.HasActorCount);
        Assert.Equal(75, signals.ActorScore);
        Assert.Equal(1, signals.HasMeasurableConstraintCount);
        Assert.Equal(25, signals.MeasurableConstraintCoverage);
        Assert.Equal(50, signals.Consistency);
        Assert.Equal(50, signals.QuestionScore);

        var bars = QualityScoreCalculator.Combine(signals, llmClarity: 80, llmSpecificity: 70);
        Assert.Equal(50, bars.Completeness); // 0.5*75 + 0.5*25
        Assert.Equal(80, bars.Clarity);
        Assert.Equal(35, bars.Testability); // 0.6*25 + 0.4*50
        Assert.Equal(50, bars.Consistency);
        Assert.Equal(73, bars.Specificity); // 0.5*75 + 0.5*70
        Assert.Equal(58, bars.Overall); // (50+80+35+50+73)/5 = 57.6 → 58
    }
}
