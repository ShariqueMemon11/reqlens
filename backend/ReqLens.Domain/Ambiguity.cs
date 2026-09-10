namespace ReqLens.Domain;

public enum AmbiguitySeverity
{
    High = 1,
    Medium = 2,
    Low = 3
}

public sealed class Ambiguity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string RequirementId { get; set; } = "";
    public AmbiguitySeverity Severity { get; set; }
    public string Issue { get; set; } = "";
    public bool IsResolved { get; set; }
    public string ResolvedSelections { get; set; } = "";
    public ICollection<AmbiguityQuestion> Questions { get; set; } = [];
    public Document Document { get; set; } = null!;
}

public sealed class AmbiguityQuestion
{
    public Guid Id { get; set; }
    public Guid AmbiguityId { get; set; }
    public string Text { get; set; } = "";
    public string OptionType { get; set; } = "checkbox";
    public int SortOrder { get; set; }
    public Ambiguity Ambiguity { get; set; } = null!;
}
