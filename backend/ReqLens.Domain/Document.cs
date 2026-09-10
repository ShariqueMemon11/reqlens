namespace ReqLens.Domain;

public enum DocumentSourceType
{
    Pdf = 1,
    PastedText = 2
}

public sealed class Document
{
    public Guid Id { get; set; }
    public DocumentSourceType SourceType { get; set; }
    public string? FileName { get; set; }
    public byte[] RawContent { get; set; } = [];
    public string ExtractedText { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Requirement> Requirements { get; set; } = [];
    public ICollection<Ambiguity> Ambiguities { get; set; } = [];
    public ICollection<Contradiction> Contradictions { get; set; } = [];
    public QualityScore? QualityScore { get; set; }
}
