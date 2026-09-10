using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReqLens.Application.Llm;

namespace ReqLens.Infrastructure.Llm;

public sealed class GroqLlmClient : ILlmClient
{
    // Groq currently documents strict constrained decoding only for GPT-OSS 20B/120B.
    private static readonly HashSet<string> StrictModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai/gpt-oss-20b",
        "openai/gpt-oss-120b"
    };

    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset _nextAllowedUtc = DateTimeOffset.MinValue;

    private readonly HttpClient _http;
    private readonly GroqLlmOptions _options;
    private readonly ILogger<GroqLlmClient> _logger;

    public GroqLlmClient(HttpClient http, IOptions<GroqLlmOptions> options, ILogger<GroqLlmClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteJsonAsync(
        LlmJsonRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new LlmException(503, "Llm:Groq:ApiKey is not configured.");
        }

        var preferStrict = _options.Strict && StrictModels.Contains(_options.Model);
        await WaitForRequestSlotAsync(cancellationToken);
        try
        {
            return await SendAsync(request, preferStrict, cancellationToken);
        }
        catch (LlmException ex) when (preferStrict && IsStrictUnsupported(ex))
        {
            // Model or account rejected constrained decoding; continue with best-effort + repair.
            return await SendAsync(request, strict: false, cancellationToken);
        }
    }

    private async Task WaitForRequestSlotAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(0, _options.MinRequestIntervalSeconds));
        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            var wait = _nextAllowedUtc - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                _logger.LogInformation("Pacing Groq request; waiting {Delay} for TPM.", wait);
                await Task.Delay(wait, cancellationToken);
            }

            _nextAllowedUtc = DateTimeOffset.UtcNow + interval;
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private async Task<string> SendAsync(
        LlmJsonRequest request,
        bool strict,
        CancellationToken cancellationToken)
    {
        var body = BuildBody(request, strict);
        const int maxAttempts = 3;
        const int maxRateLimitAttempts = 8;
        HttpResponseMessage? response = null;
        var payload = "";

        try
        {
            for (var attempt = 1; ; attempt++)
            {
                response?.Dispose();
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                response = await _http.SendAsync(httpRequest, cancellationToken);
                payload = await response.Content.ReadAsStringAsync(cancellationToken);
                var rateLimited = response.StatusCode == HttpStatusCode.TooManyRequests;
                var attemptLimit = rateLimited ? maxRateLimitAttempts : maxAttempts;
                if (response.IsSuccessStatusCode
                    || !IsTransient(response.StatusCode, payload)
                    || attempt >= attemptLimit)
                {
                    break;
                }

                var delay = rateLimited
                    ? RateLimitDelay(response, payload, attempt)
                    : TimeSpan.FromSeconds(attempt);
                // TPD waits are often many minutes (Retry-After: 10–20m). Sitting that long
                // just hits HttpClient.Timeout and the eval records a bland cancel, not 429.
                if (rateLimited && delay > TimeSpan.FromSeconds(25))
                {
                    _logger.LogWarning(
                        "Groq 429 asked for {Delay}, which exceeds the retry cap. Failing the call instead of waiting. {Message}",
                        delay,
                        GroqErrorMessage(payload, response.StatusCode));
                    break;
                }

                _logger.LogWarning(
                    "Groq {Status} on attempt {Attempt}/{Limit}; waiting {Delay} then retrying. {Message}",
                    (int)response.StatusCode,
                    attempt,
                    attemptLimit,
                    delay,
                    GroqErrorMessage(payload, response.StatusCode));
                await Task.Delay(delay, cancellationToken);
            }

            if (!response!.IsSuccessStatusCode)
            {
                // failed_generation is only a salvage path for constrained-decode 400s.
                // A 429 body must not be treated as a successful (possibly empty) JSON result —
                // that is how an eval run quietly records misses instead of a rate-limit failure.
                if (response.StatusCode != HttpStatusCode.TooManyRequests
                    && TryReadFailedGeneration(payload, out var generated))
                {
                    return generated;
                }

                throw new LlmException(
                    MapStatus(response.StatusCode),
                    GroqErrorMessage(payload, response.StatusCode));
            }

            return ReadContent(payload);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private string BuildBody(LlmJsonRequest request, bool strict)
    {
        var messages = new JsonArray();
        foreach (var message in request.Messages)
        {
            messages.Add(new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content
            });
        }

        var schemaNode = JsonNode.Parse(request.SchemaJson)
            ?? throw new LlmException(500, "Requirements JSON schema could not be parsed.");

        var root = new JsonObject
        {
            ["model"] = _options.Model,
            // temperature 0 + seed: Groq's best-effort determinism for identical prompts.
            ["temperature"] = 0,
            ["seed"] = 0,
            ["include_reasoning"] = false,
            ["messages"] = messages,
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = request.SchemaName,
                    ["strict"] = strict,
                    ["schema"] = schemaNode
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(request.ReasoningEffort))
        {
            root["reasoning_effort"] = request.ReasoningEffort;
        }

        return root.ToJsonString();
    }

    private static string ReadContent(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new LlmException(502, "Groq returned no choices.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new LlmException(502, "Groq returned an empty message.");
        }

        return content.Trim();
    }

    private static TimeSpan RateLimitDelay(HttpResponseMessage response, string payload, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } header && header > TimeSpan.Zero)
        {
            return header + TimeSpan.FromMilliseconds(250);
        }

        var match = Regex.Match(payload, @"try again in (\d+(?:\.\d+)?)\s*s", RegexOptions.IgnoreCase);
        if (match.Success
            && double.TryParse(
                match.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds)
            && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds + 0.5);
        }

        return TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt)));
    }

    private static bool IsTransient(HttpStatusCode status, string payload)
    {
        if (status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout)
        {
            return true;
        }

        return status == HttpStatusCode.BadRequest
            && payload.Contains("json_validate_failed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStrictUnsupported(LlmException ex)
    {
        var text = ex.Message;
        return text.Contains("strict", StringComparison.OrdinalIgnoreCase)
            || text.Contains("constrained", StringComparison.OrdinalIgnoreCase)
            || text.Contains("json_schema", StringComparison.OrdinalIgnoreCase);
    }

    private static int MapStatus(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests ? 429 : 502;

    private static bool TryReadFailedGeneration(string payload, out string generated)
    {
        generated = "";
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("error", out var error)
                || !error.TryGetProperty("failed_generation", out var failed))
            {
                return false;
            }

            var trimmed = failed.GetString()?.Trim() ?? "";
            if (trimmed.Length == 0 || trimmed[0] is not '{' and not '[')
            {
                return false;
            }

            generated = trimmed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string GroqErrorMessage(string payload, HttpStatusCode status)
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
            // Fall through to the generic message.
        }

        return $"Groq request failed ({(int)status}).";
    }
}
