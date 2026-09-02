using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryService.Security;

// A separate, minimal authentication scheme used ONLY by the internal
// reduce-stock / restore-stock endpoints. These endpoints are called by
// Order Service on behalf of a user who has already been authenticated and
// authorized to place an order - they are not meant to be reachable by end
// users directly, so they deliberately do not go through the normal JWT +
// role-based flow. This keeps "only Admin can add/update stock" (enforced
// on the public Create/Update endpoints) separate from "the order pipeline
// is allowed to adjust stock" (enforced here via a shared secret header).
public class InternalApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "InternalApiKey";
    private const string HeaderName = "X-Internal-Api-Key";

    private readonly InternalApiKeySettings _settings;

    public InternalApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        InternalApiKeySettings settings)
        : base(options, logger, encoder)
    {
        _settings = settings;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {HeaderName} header."));
        }

        if (string.IsNullOrEmpty(_settings.ApiKey) || providedKey != _settings.ApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));
        }

        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.Name, "internal-service"));
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
