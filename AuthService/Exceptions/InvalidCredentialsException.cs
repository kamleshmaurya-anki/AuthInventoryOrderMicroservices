using Shared.Exceptions;

namespace AuthService.Exceptions;

public class InvalidCredentialsException : UnauthorizedAppException
{
    public InvalidCredentialsException()
        : base("Invalid username or password.")
    {
    }
}
