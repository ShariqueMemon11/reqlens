namespace ReqLens.Application.Documents;

public interface IPdfTextExtractor
{
    string Extract(byte[] pdfBytes);
}
