namespace ReqLens.Application.Llm;

public sealed record LlmMessage(string Role, string Content);

public sealed record LlmJsonRequest(
    IReadOnlyList<LlmMessage> Messages,
    string SchemaJson,
    string SchemaName,
    string? ReasoningEffort = null);

public interface ILlmClient
{
    Task<string> CompleteJsonAsync(LlmJsonRequest request, CancellationToken cancellationToken);
}
