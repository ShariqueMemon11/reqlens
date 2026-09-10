using Microsoft.AspNetCore.Mvc;
using ReqLens.Application.Documents;
using ReqLens.Application.Llm;

namespace ReqLens.Api.Controllers;

[ApiController]
[Route("documents")]
[RequestSizeLimit(DocumentUploadService.MaxUploadBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = DocumentUploadService.MaxUploadBytes)]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentUploadService _upload;
    private readonly IRequirementExtractionService _extraction;
    private readonly IAmbiguityDetectionService _ambiguities;
    private readonly IContradictionDetectionService _contradictions;
    private readonly IQualityScoringService _quality;
    private readonly IIssueResolutionService _resolution;

    public DocumentsController(
        IDocumentUploadService upload,
        IRequirementExtractionService extraction,
        IAmbiguityDetectionService ambiguities,
        IContradictionDetectionService contradictions,
        IQualityScoringService quality,
        IIssueResolutionService resolution)
    {
        _upload = upload;
        _extraction = extraction;
        _ambiguities = ambiguities;
        _contradictions = contradictions;
        _quality = quality;
        _resolution = resolution;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadDocumentResult>> Post(
        [FromForm] IFormFile? file,
        [FromForm] string? text,
        CancellationToken cancellationToken)
    {
        byte[]? fileBytes = null;
        if (file is not null)
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            fileBytes = memory.ToArray();
        }

        var request = new DocumentUploadRequest(
            fileBytes,
            file?.FileName,
            file?.ContentType,
            text);

        try
        {
            var result = await _upload.UploadAsync(request, cancellationToken);
            return Created($"/documents/{result.Id}", result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }

    [HttpPost("{id:guid}/extract")]
    public async Task<ActionResult<ExtractRequirementsResult>> Extract(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _extraction.ExtractAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (LlmException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }

    [HttpPost("{id:guid}/ambiguities")]
    public async Task<ActionResult<DetectAmbiguitiesResult>> DetectAmbiguities(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ambiguities.DetectAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (LlmException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }

    [HttpPost("{id:guid}/contradictions")]
    public async Task<ActionResult<DetectContradictionsResult>> DetectContradictions(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contradictions.DetectAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (LlmException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }

    [HttpPost("{id:guid}/score")]
    public async Task<ActionResult<ComputeQualityScoreResult>> Score(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _quality.ScoreAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (LlmException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }

    [HttpPost("{id:guid}/ambiguities/{ambiguityId:guid}/resolve")]
    public async Task<ActionResult<ResolveIssueResult>> ResolveAmbiguity(
        Guid id,
        Guid ambiguityId,
        [FromBody] ResolveAmbiguityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resolution.ResolveAmbiguityAsync(id, ambiguityId, request, cancellationToken);
            return Ok(result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (LlmException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }

    [HttpPost("{id:guid}/contradictions/{contradictionId:guid}/resolve")]
    public async Task<ActionResult<ResolveIssueResult>> ResolveContradiction(
        Guid id,
        Guid contradictionId,
        [FromBody] ResolveContradictionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resolution.ResolveContradictionAsync(id, contradictionId, request, cancellationToken);
            return Ok(result);
        }
        catch (UploadDocumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (LlmException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
    }
}
