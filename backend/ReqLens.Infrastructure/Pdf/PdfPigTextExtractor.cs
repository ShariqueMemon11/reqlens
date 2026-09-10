using ReqLens.Application.Documents;
using UglyToad.PdfPig;

namespace ReqLens.Infrastructure.Pdf;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public string Extract(byte[] pdfBytes)
    {
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var pages = document.GetPages().Select(page => page.Text);
            return string.Join('\n', pages);
        }
        catch (Exception ex) when (ex is not UploadDocumentException)
        {
            throw new UploadDocumentException(
                400,
                "This PDF could not be read. It may be corrupt or encrypted.");
        }
    }
}
