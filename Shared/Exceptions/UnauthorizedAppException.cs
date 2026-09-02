namespace Shared.Exceptions;

public class UnauthorizedAppException : AppExceptionBase
{
    public UnauthorizedAppException(string message) : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}
