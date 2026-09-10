using System.Text.Json.Serialization;

namespace ReqLens.Application.Documents;

public sealed class RequirementsExtractionDto
{
    [JsonPropertyName("requirements")]
    public List<RequirementItemDto> Requirements { get; set; } = [];
}

public sealed class RequirementItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("source_line")]
    public int SourceLine { get; set; }
}
