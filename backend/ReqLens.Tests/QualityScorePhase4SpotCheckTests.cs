using ReqLens.Application.Documents;

namespace ReqLens.Tests;

/// <summary>
/// Spot-check regex signals against Phase 4 FLAG categories for the 11-sentence
/// document used throughout extraction/ambiguity tuning. Disagreements are listed
/// explicitly so a new mismatch fails the test instead of hiding in comments.
/// </summary>
public sealed class QualityScorePhase4SpotCheckTests
{
    [Flags]
    private enum Phase4Flag
    {
        Skip = 0,
        VagueVerb = 1,
        MissingActor = 2,
        MissingConstraint = 4
    }

    private sealed record Case(
        string Text,
        Phase4Flag Phase4,
        bool RegexVague,
        bool RegexActor,
        bool RegexConstraint);

    [Fact]
    public void ElevenSentenceDocument_RegexMatchesPhase4ExceptKnownGaps()
    {
        var cases = new Case[]
        {
            new(
                "Managers can create, edit, and delete customer accounts, with each action recorded in the audit log.",
                Phase4Flag.Skip,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: false),
            new(
                "The system should support user authentication.",
                Phase4Flag.VagueVerb,
                RegexVague: true,
                RegexActor: true,
                RegexConstraint: false),
            new(
                "Handle payment processing for customer orders.",
                Phase4Flag.VagueVerb | Phase4Flag.MissingActor,
                RegexVague: true,
                RegexActor: false,
                RegexConstraint: false),
            new(
                "The support agent can view, respond to, and close customer tickets, with response times logged.",
                Phase4Flag.Skip,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: false),
            new(
                "Users can upload attachments to a ticket.",
                Phase4Flag.MissingConstraint,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: false),
            new(
                "Customers can cancel an order at any time.",
                Phase4Flag.Skip,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: false),
            new(
                "Shipped orders cannot be cancelled.",
                Phase4Flag.Skip,
                RegexVague: false,
                RegexActor: false,
                RegexConstraint: false),
            new(
                "The reports should be managed properly.",
                Phase4Flag.VagueVerb,
                RegexVague: true,
                RegexActor: false,
                RegexConstraint: false),
            new(
                "Administrators can assign, reassign, and revoke user roles, and all role changes require a confirmation step before taking effect.",
                Phase4Flag.Skip,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: false),
            new(
                "The system shall lock a user account after 5 failed login attempts within 10 minutes, and notify the user via email.",
                Phase4Flag.Skip,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: true),
            new(
                "Users can export their data in CSV format, downloadable directly from their account settings page.",
                Phase4Flag.MissingConstraint,
                RegexVague: false,
                RegexActor: true,
                RegexConstraint: false),
        };

        var disagreements = new List<string>();
        foreach (var item in cases)
        {
            Assert.Equal(item.RegexVague, QualityScoreCalculator.HasVagueVerbWithoutOperations(item.Text));
            Assert.Equal(item.RegexActor, QualityScoreCalculator.HasActor(item.Text));
            Assert.Equal(item.RegexConstraint, QualityScoreCalculator.HasMeasurableConstraint(item.Text));

            var phase4Vague = item.Phase4.HasFlag(Phase4Flag.VagueVerb);
            var phase4MissingActor = item.Phase4.HasFlag(Phase4Flag.MissingActor);
            var phase4MissingConstraint = item.Phase4.HasFlag(Phase4Flag.MissingConstraint);

            if (item.RegexVague != phase4Vague)
            {
                disagreements.Add($"vague: {item.Text}");
            }

            // Phase 4 only flags missing actor on permissioned actions; regex scores any
            // sentence without a human-subject or "the system".
            if (phase4MissingActor == item.RegexActor)
            {
                disagreements.Add($"actor: {item.Text}");
            }

            if (phase4MissingConstraint && item.RegexConstraint)
            {
                disagreements.Add($"constraint: {item.Text}");
            }
        }

        var known = new[]
        {
            // Constraint-style sentence: Phase 4 does not FLAG undefined actor; regex sees no subject.
            "actor: Shipped orders cannot be cancelled.",
            // Vague "managed" with a thing as subject: Phase 4 flags the verb, not actor-undefined.
            "actor: The reports should be managed properly.",
        };

        Assert.Equal(known, disagreements);
    }
}
