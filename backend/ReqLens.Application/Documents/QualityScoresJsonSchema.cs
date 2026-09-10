namespace ReqLens.Application.Documents;

public static class QualityScoresJsonSchema
{
    public const string Name = "quality_sub_scores";

    public const string Json = """
        {
          "type": "object",
          "properties": {
            "clarity": { "type": "integer", "minimum": 0, "maximum": 100 },
            "specificity": { "type": "integer", "minimum": 0, "maximum": 100 }
          },
          "required": ["clarity", "specificity"],
          "additionalProperties": false
        }
        """;
}
