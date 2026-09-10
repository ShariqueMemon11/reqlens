namespace ReqLens.Application;

public sealed record DocumentUploadRequest(
    byte[]? FileBytes,
    string? FileName,
    string? ContentType,
    string? Text);

public sealed record UploadDocumentResult(
    Guid Id,
    int ExtractedTextLength,
    string ExtractedTextPreview);
