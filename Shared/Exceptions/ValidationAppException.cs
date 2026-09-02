namespace Shared.Exceptions;

public class ValidationAppException : AppExceptionBase
{
    public ValidationAppException(string message) : base(message, StatusCodes.Status400BadRequest)
    {
    }
}
