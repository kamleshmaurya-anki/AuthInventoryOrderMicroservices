namespace Shared.Exceptions;

public class ForbiddenAppException : AppExceptionBase
{
    public ForbiddenAppException(string message) : base(message, StatusCodes.Status403Forbidden)
    {
    }
}
