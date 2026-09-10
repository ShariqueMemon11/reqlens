namespace ReqLens.Infrastructure.Llm;

public sealed class GeminiEmbeddingOptions
{
    public const string SectionName = "Llm:Gemini";

    public string ApiKey { get; set; } = "";
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
}
