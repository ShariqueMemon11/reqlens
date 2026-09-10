namespace ReqLens.Application.Documents;

public static class RequirementsJsonSchema
{
    public const string Name = "requirements_extraction";

    // Groq strict mode requires every property in `required` and additionalProperties: false.
    public const string Json = """
        {
          "type": "object",
          "properties": {
            "requirements": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "id": { "type": "string" },
                  "text": { "type": "string" },
                  "source_line": { "type": "integer" }
                },
                "required": ["id", "text", "source_line"],
                "additionalProperties": false
              }
            }
          },
          "required": ["requirements"],
          "additionalProperties": false
        }
        """;
}
