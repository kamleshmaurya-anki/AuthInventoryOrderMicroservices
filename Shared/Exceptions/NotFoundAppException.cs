namespace Shared.Exceptions;

public class NotFoundAppException : AppExceptionBase
{
    public NotFoundAppException(string message) : base(message, StatusCodes.Status404NotFound)
    {
    }
}
