namespace Epoxid.CodeGenV2;

public class SyntaxErrorException : Exception
{
    public SyntaxErrorException()
        : base()
    {
    }

    public SyntaxErrorException(string? message)
        : base(message)
    {
    }

    public SyntaxErrorException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
