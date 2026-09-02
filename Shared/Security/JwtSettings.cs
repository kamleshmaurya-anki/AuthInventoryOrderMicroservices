namespace Shared.Security;

// Bound from the "Jwt" section of appsettings.json. The Key/Issuer/Audience
// MUST be identical across AuthService, InventoryService and OrderService -
// Auth Service signs tokens with this key, the other two validate tokens
// with the same key. No network round-trip to Auth Service is needed to
// validate a token.
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
