using Shared.Exceptions;

namespace AuthService.Exceptions;

public class InvalidRoleException : ValidationAppException
{
    public InvalidRoleException(string role)
        : base($"'{role}' is not a valid role. Allowed values: ADMIN, USER.")
    {
    }
}
