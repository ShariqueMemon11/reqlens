using System.Text;
using ReqLens.Domain;

namespace ReqLens.Application.Documents;

public sealed class DocumentUploadService : IDocumentUploadService
{
    public const int PreviewLength = 500;
    public const int MaxUploadBytes = 10 * 1024 * 1024;

    private static readonly byte[] PdfMagic = "%PDF"u8.ToArray();

    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentRepository _documents;

    public DocumentUploadService(
        IPdfTextExtractor pdfTextExtractor,
        IDocumentRepository documents)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _documents = documents;
    }

    public async Task<UploadDocumentResult> UploadAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        var hasFile = request.FileBytes is { Length: > 0 };
        var hasTextField = request.Text is not null;

        if (hasFile == hasTextField)
        {
            throw new UploadDocumentException(
                400,
                "Send either a PDF file or pasted text, not both or neither.");
        }

        Document document;
        if (hasFile)
        {
            document = FromPdf(request);
        }
        else
        {
            document = FromPastedText(request.Text!);
        }

        if (string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            throw new UploadDocumentException(
                400,
                "No text could be extracted. The file may be empty, image-only, or the pasted text was blank.");
        }

        await _documents.AddAsync(document, cancellationToken);

        var extracted = document.ExtractedText;
        var previewLength = Math.Min(PreviewLength, extracted.Length);

        return new UploadDocumentResult(
            document.Id,
            extracted.Length,
            extracted[..previewLength]);
    }

    private Document FromPdf(DocumentUploadRequest request)
    {
        var bytes = request.FileBytes!;
        if (bytes.Length > MaxUploadBytes)
        {
            throw new UploadDocumentException(400, "PDF must be 10MB or smaller.");
        }

        if (!LooksLikePdf(bytes, request.FileName, request.ContentType))
        {
            throw new UploadDocumentException(400, "Only PDF files are accepted.");
        }

        var extracted = _pdfTextExtractor.Extract(bytes);

        return new Document
        {
            Id = Guid.NewGuid(),
            SourceType = DocumentSourceType.Pdf,
            FileName = request.FileName,
            RawContent = bytes,
            ExtractedText = extracted.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static Document FromPastedText(string text)
    {
        var extracted = text.Trim();
        if (extracted.Length == 0)
        {
            throw new UploadDocumentException(400, "Pasted text is empty.");
        }

        return new Document
        {
            Id = Guid.NewGuid(),
            SourceType = DocumentSourceType.PastedText,
            FileName = null,
            RawContent = Encoding.UTF8.GetBytes(text),
            ExtractedText = extracted,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static bool LooksLikePdf(byte[] bytes, string? fileName, string? contentType)
    {
        if (bytes.Length >= PdfMagic.Length && bytes.AsSpan(0, PdfMagic.Length).SequenceEqual(PdfMagic))
        {
            return true;
        }

        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName is not null
            && fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
