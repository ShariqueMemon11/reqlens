namespace ReqLens.Application.Documents;

public sealed class UploadDocumentException : Exception
{
    public int StatusCode { get; }

    public UploadDocumentException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
