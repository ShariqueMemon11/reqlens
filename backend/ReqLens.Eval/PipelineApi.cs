using System.Text.Json;

namespace ReqLens.Eval;

public sealed class PipelineApi
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PipelineApi(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task<UploadResult> UploadTextAsync(string text, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(text), "text");
        using var response = await _http.PostAsync("documents", form, cancellationToken);
        return await Read<UploadResult>(response, cancellationToken);
    }

    public Task<ExtractResult> ExtractAsync(Guid documentId, CancellationToken cancellationToken) =>
        Post<ExtractResult>($"documents/{documentId}/extract", cancellationToken);

    public Task<AmbiguityResult> DetectAmbiguitiesAsync(Guid documentId, CancellationToken cancellationToken) =>
        Post<AmbiguityResult>($"documents/{documentId}/ambiguities", cancellationToken);

    public Task<ContradictionResult> DetectContradictionsAsync(Guid documentId, CancellationToken cancellationToken) =>
        Post<ContradictionResult>($"documents/{documentId}/contradictions", cancellationToken);

    private async Task<T> Post<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(path, content: null, cancellationToken);
        return await Read<T>(response, cancellationToken);
    }

    private async Task<T> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{(int)response.StatusCode} {response.RequestMessage?.RequestUri}: {body}");
        }

        return JsonSerializer.Deserialize<T>(body, _json)
            ?? throw new InvalidOperationException($"Empty JSON from {response.RequestMessage?.RequestUri}");
    }
}

public sealed class UploadResult
{
    public Guid Id { get; set; }
}

public sealed class ExtractResult
{
    public List<RequirementDto> Requirements { get; set; } = [];
}

public sealed class RequirementDto
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class AmbiguityResult
{
    public List<AmbiguityDto> Ambiguities { get; set; } = [];
}

public sealed class AmbiguityDto
{
    public string RequirementId { get; set; } = "";
    public string Issue { get; set; } = "";
}

public sealed class ContradictionResult
{
    public List<ContradictionDto> Contradictions { get; set; } = [];
}

public sealed class ContradictionDto
{
    public List<string> RequirementIds { get; set; } = [];
}
