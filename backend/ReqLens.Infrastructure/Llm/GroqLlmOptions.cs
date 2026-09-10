namespace ReqLens.Infrastructure.Llm;

public sealed class GroqLlmOptions
{
    public const string SectionName = "Llm:Groq";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Extra gap before each Groq call. 0 keeps the UI snappy (429 Retry-After still applies).
    /// Set ~12 for an eval run: free-tier TPM is 8000, and one extract prompt is often 1500–2500 tokens.
    /// </summary>
    public int MinRequestIntervalSeconds { get; set; }
}
