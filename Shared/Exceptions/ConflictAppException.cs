namespace Shared.Exceptions;

public class ConflictAppException : AppExceptionBase
{
    public ConflictAppException(string message) : base(message, StatusCodes.Status409Conflict)
    {
    }
}
