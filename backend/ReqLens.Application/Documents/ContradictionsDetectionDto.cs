using System.Text.Json.Serialization;

namespace ReqLens.Application.Documents;

public sealed class ContradictionsDetectionDto
{
    [JsonPropertyName("contradictions")]
    public List<ContradictionItemDto> Contradictions { get; set; } = [];
}

public sealed class ContradictionItemDto
{
    [JsonPropertyName("requirement_ids")]
    public List<string> RequirementIds { get; set; } = [];

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("resolution_question")]
    public string ResolutionQuestion { get; set; } = "";
}
