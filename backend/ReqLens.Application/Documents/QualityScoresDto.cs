using System.Text.Json.Serialization;

namespace ReqLens.Application.Documents;

public sealed class QualityLlmScoresDto
{
    [JsonPropertyName("clarity")]
    public int Clarity { get; set; }

    [JsonPropertyName("specificity")]
    public int Specificity { get; set; }
}
