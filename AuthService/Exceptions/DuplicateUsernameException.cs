using Shared.Exceptions;

namespace AuthService.Exceptions;

public class DuplicateUsernameException : ConflictAppException
{
    public DuplicateUsernameException(string username)
        : base($"Username '{username}' is already taken.")
    {
    }
}
