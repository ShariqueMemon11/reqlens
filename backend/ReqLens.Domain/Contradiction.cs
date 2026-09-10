namespace ReqLens.Domain;

public sealed class Contradiction
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string RequirementIdA { get; set; } = "";
    public string RequirementIdB { get; set; } = "";
    public string Description { get; set; } = "";
    public string ResolutionQuestion { get; set; } = "";
    public bool IsResolved { get; set; }
    public string ChosenRequirementId { get; set; } = "";
    public Document Document { get; set; } = null!;
}
