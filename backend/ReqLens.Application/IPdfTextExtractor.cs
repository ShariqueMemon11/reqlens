namespace ReqLens.Application;

public interface IPdfTextExtractor
{
    string Extract(byte[] pdfBytes);
}
