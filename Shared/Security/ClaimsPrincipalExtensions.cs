using System.Security.Claims;

namespace Shared.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtClaimTypes.UserId);
        if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("Token does not contain a valid user id claim.");
        }

        return userId;
    }

    public static string GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtClaimTypes.Username) ?? string.Empty;
    }

    public static string GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtClaimTypes.Role) ?? string.Empty;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return string.Equals(principal.GetRole(), Constants.Roles.Admin, StringComparison.OrdinalIgnoreCase);
    }
}
