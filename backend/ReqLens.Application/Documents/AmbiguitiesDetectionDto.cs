using System.Text.Json.Serialization;

namespace ReqLens.Application.Documents;

public sealed class AmbiguitiesDetectionDto
{
    [JsonPropertyName("ambiguities")]
    public List<AmbiguityItemDto> Ambiguities { get; set; } = [];
}

public sealed class AmbiguityItemDto
{
    [JsonPropertyName("requirement_id")]
    public string RequirementId { get; set; } = "";

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "";

    [JsonPropertyName("issue")]
    public string Issue { get; set; } = "";

    [JsonPropertyName("questions")]
    public List<AmbiguityQuestionItemDto> Questions { get; set; } = [];
}

public sealed class AmbiguityQuestionItemDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("option_type")]
    public string OptionType { get; set; } = "checkbox";
}
