using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ReqLens.Application.Llm;

namespace ReqLens.Infrastructure.Llm;

public sealed class GeminiEmbeddingClient : IEmbeddingClient
{
    private const int BatchSize = 50;

    private readonly HttpClient _http;
    private readonly GeminiEmbeddingOptions _options;

    public GeminiEmbeddingClient(HttpClient http, IOptions<GeminiEmbeddingOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new LlmException(503, "Llm:Gemini:ApiKey is not configured.");
        }

        var vectors = new List<float[]>(texts.Count);
        for (var offset = 0; offset < texts.Count; offset += BatchSize)
        {
            var batch = texts.Skip(offset).Take(BatchSize).ToList();
            vectors.AddRange(await EmbedBatchAsync(batch, cancellationToken));
        }

        return vectors;
    }

    private async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var requests = new JsonArray();
        foreach (var text in texts)
        {
            requests.Add(new JsonObject
            {
                ["model"] = "models/" + _options.EmbeddingModel,
                ["content"] = new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = text }
                    }
                },
                ["taskType"] = "SEMANTIC_SIMILARITY",
                ["outputDimensionality"] = 768
            });
        }

        var body = new JsonObject { ["requests"] = requests }.ToJsonString();
        var path = $"models/{_options.EmbeddingModel}:batchEmbedContents";

        const int maxAttempts = 3;
        HttpResponseMessage? response = null;
        var payload = "";

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

            response = await _http.SendAsync(httpRequest, cancellationToken);
            payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode || !IsTransient(response.StatusCode) || attempt == maxAttempts)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
        }

        if (!response!.IsSuccessStatusCode)
        {
            throw new LlmException(
                response.StatusCode == HttpStatusCode.TooManyRequests ? 429 : 502,
                GeminiErrorMessage(payload, response.StatusCode));
        }

        return ReadEmbeddings(payload, texts.Count);
    }

    private static List<float[]> ReadEmbeddings(string payload, int expectedCount)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("embeddings", out var embeddings))
        {
            throw new LlmException(502, "Gemini returned no embeddings.");
        }

        var vectors = new List<float[]>(expectedCount);
        foreach (var item in embeddings.EnumerateArray())
        {
            if (!item.TryGetProperty("values", out var values))
            {
                throw new LlmException(502, "Gemini embedding was missing values.");
            }

            vectors.Add(values.EnumerateArray().Select(value => value.GetSingle()).ToArray());
        }

        if (vectors.Count != expectedCount)
        {
            throw new LlmException(502, "Gemini embedding batch size did not match the request.");
        }

        return vectors;
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private static string GeminiErrorMessage(string payload, HttpStatusCode status)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                && message.GetString() is { Length: > 0 } text)
            {
                return text;
            }
        }
        catch (JsonException)
        {
            // Fall through.
        }

        return $"Gemini embedding request failed ({(int)status}).";
    }
}
