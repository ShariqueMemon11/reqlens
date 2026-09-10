namespace ReqLens.Application.Llm;

public sealed class LlmException : Exception
{
    public int StatusCode { get; }

    public LlmException(int statusCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
