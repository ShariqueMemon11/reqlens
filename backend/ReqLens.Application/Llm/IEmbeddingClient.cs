namespace ReqLens.Application.Llm;

public interface IEmbeddingClient
{
    Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken);
}
