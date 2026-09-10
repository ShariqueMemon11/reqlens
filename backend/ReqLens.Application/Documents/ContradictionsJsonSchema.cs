namespace ReqLens.Application.Documents;

public static class ContradictionsJsonSchema
{
    public const string Name = "contradiction_detection";

    public const string Json = """
        {
          "type": "object",
          "properties": {
            "contradictions": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "requirement_ids": {
                    "type": "array",
                    "items": { "type": "string" }
                  },
                  "description": { "type": "string" },
                  "resolution_question": { "type": "string" }
                },
                "required": ["requirement_ids", "description", "resolution_question"],
                "additionalProperties": false
              }
            }
          },
          "required": ["contradictions"],
          "additionalProperties": false
        }
        """;
}
