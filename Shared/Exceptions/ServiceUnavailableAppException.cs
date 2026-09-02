namespace Shared.Exceptions;

// Raised when a downstream microservice call fails (network error, timeout).
public class ServiceUnavailableAppException : AppExceptionBase
{
    public ServiceUnavailableAppException(string message) : base(message, StatusCodes.Status503ServiceUnavailable)
    {
    }
}
