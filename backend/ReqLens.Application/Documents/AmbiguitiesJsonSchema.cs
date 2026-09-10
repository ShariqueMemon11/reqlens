namespace ReqLens.Application.Documents;

public static class AmbiguitiesJsonSchema
{
    public const string Name = "ambiguity_detection";

    public const string Json = """
        {
          "type": "object",
          "properties": {
            "ambiguities": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "requirement_id": { "type": "string" },
                  "severity": { "type": "string", "enum": ["high", "medium", "low"] },
                  "issue": { "type": "string" },
                  "questions": {
                    "type": "array",
                    "items": {
                      "type": "object",
                      "properties": {
                        "text": { "type": "string" },
                        "option_type": { "type": "string", "enum": ["checkbox"] }
                      },
                      "required": ["text", "option_type"],
                      "additionalProperties": false
                    }
                  }
                },
                "required": ["requirement_id", "severity", "issue", "questions"],
                "additionalProperties": false
              }
            }
          },
          "required": ["ambiguities"],
          "additionalProperties": false
        }
        """;
}
