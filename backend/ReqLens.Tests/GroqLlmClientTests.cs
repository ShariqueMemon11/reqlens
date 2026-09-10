using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReqLens.Application.Llm;
using ReqLens.Infrastructure.Llm;

namespace ReqLens.Tests;

public sealed class GroqLlmClientTests
{
    private static readonly LlmJsonRequest Request = new(
        [new LlmMessage("user", "extract")],
        """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"]}""",
        "test_schema");

    [Fact]
    public async Task RateLimit_ExhaustedRetries_Throws429_DoesNotReturnFailedGeneration()
    {
        var emptyRequirements = """{"requirements":[]}""";
        var handler = new ScriptedHandler();
        for (var i = 0; i < 8; i++)
        {
            handler.Enqueue(RateLimit(failedGeneration: emptyRequirements));
        }

        var client = CreateClient(handler);
        var ex = await Assert.ThrowsAsync<LlmException>(() =>
            client.CompleteJsonAsync(Request, CancellationToken.None));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(8, handler.Calls);
        Assert.DoesNotContain("requirements", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RateLimit_LongRetryAfter_Throws429WithoutWaiting()
    {
        var handler = new ScriptedHandler();
        var limited = RateLimit();
        limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMinutes(10));
        handler.Enqueue(limited);

        var client = CreateClient(handler);
        var ex = await Assert.ThrowsAsync<LlmException>(() =>
            client.CompleteJsonAsync(Request, CancellationToken.None));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task RateLimit_ThenSuccess_ReturnsContent()
    {
        var handler = new ScriptedHandler();
        handler.Enqueue(RateLimit());
        handler.Enqueue(RateLimit());
        handler.Enqueue(Success("""{"ok":true}"""));

        var client = CreateClient(handler);
        var json = await client.CompleteJsonAsync(Request, CancellationToken.None);

        Assert.Equal("""{"ok":true}""", json);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task BadRequest_FailedGeneration_IsSalvaged()
    {
        var handler = new ScriptedHandler();
        handler.Enqueue(Json(
            HttpStatusCode.BadRequest,
            """{"error":{"message":"json_validate_failed","failed_generation":"{\"ok\":false}"}}"""));
        handler.Enqueue(Json(
            HttpStatusCode.BadRequest,
            """{"error":{"message":"json_validate_failed","failed_generation":"{\"ok\":false}"}}"""));
        handler.Enqueue(Json(
            HttpStatusCode.BadRequest,
            """{"error":{"message":"json_validate_failed","failed_generation":"{\"ok\":false}"}}"""));

        var client = CreateClient(handler);
        var json = await client.CompleteJsonAsync(Request, CancellationToken.None);

        Assert.Equal("""{"ok":false}""", json);
    }

    private static GroqLlmClient CreateClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.groq.com/openai/v1/") };
        var options = Options.Create(new GroqLlmOptions
        {
            ApiKey = "test-key",
            Model = "openai/gpt-oss-20b",
            Strict = false
        });
        return new GroqLlmClient(http, options, NullLogger<GroqLlmClient>.Instance);
    }

    private static HttpResponseMessage RateLimit(string? failedGeneration = null)
    {
        var extra = failedGeneration is null
            ? ""
            : $",\"failed_generation\":{JsonEscape(failedGeneration)}";
        return Json(
            HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"Rate limit reached. Please try again in 0.01s\"" + extra + "}}");
    }

    private static HttpResponseMessage Success(string content) =>
        Json(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":" + JsonEscape(content) + "}}]}");

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static string JsonEscape(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public int Calls { get; private set; }

        public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException($"Unexpected Groq call #{Calls}.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
