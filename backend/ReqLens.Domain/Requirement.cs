namespace ReqLens.Domain;

public sealed class Requirement
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string RequirementId { get; set; } = "";
    public string Text { get; set; } = "";
    public int SourceLine { get; set; }
    public Document Document { get; set; } = null!;
}
