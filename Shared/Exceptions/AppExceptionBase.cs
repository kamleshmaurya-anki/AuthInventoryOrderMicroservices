namespace Shared.Exceptions;

// Base for exceptions that the global exception middleware maps to a
// specific HTTP status code, instead of a generic 500.
public abstract class AppExceptionBase : Exception
{
    public int StatusCode { get; }

    protected AppExceptionBase(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
