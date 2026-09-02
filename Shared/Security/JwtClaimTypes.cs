namespace Shared.Security;

// Centralizes the claim type names so Auth Service (issuer) and the other
// services (consumers) always agree on what to call things.
public static class JwtClaimTypes
{
    public const string UserId = "uid";
    public const string Username = "username";
    public const string Role = "role";
}
