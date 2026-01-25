using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReservationService.Tests.Integration;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no userId header is present, fail authentication
        if (!Context.Request.Headers.TryGetValue("X-Test-UserId", out var userId) || string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        // Check if custom claims are provided in the request headers
        if (Context.Request.Headers.TryGetValue("X-Test-Role", out var role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        if (Context.Request.Headers.TryGetValue("X-Test-Username", out var username))
        {
            claims.Add(new Claim(ClaimTypes.Name, username.ToString()));
        }

        if (Context.Request.Headers.TryGetValue("X-Test-FirstName", out var firstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, firstName.ToString()));
        }

        if (Context.Request.Headers.TryGetValue("X-Test-LastName", out var lastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, lastName.ToString()));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
