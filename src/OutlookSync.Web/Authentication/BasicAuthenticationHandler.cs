using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace OutlookSync.Web.Authentication;

/// <summary>
/// ASP.NET Core authentication handler that validates HTTP Basic authentication credentials
/// against the values configured in <see cref="BasicAuthSettings"/>.
/// When no username is configured, all requests are authenticated automatically.
/// </summary>
public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<BasicAuthSettings> basicAuthSettings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The authentication scheme name used when registering the handler.</summary>
    public const string SchemeName = "BasicAuth";

    private readonly BasicAuthSettings _settings = basicAuthSettings.Value;

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authentication disabled – allow every request through.
        if (string.IsNullOrEmpty(_settings.Username))
        {
            var anonymous = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "anonymous")], Scheme.Name));
            return Task.FromResult(
                AuthenticateResult.Success(new AuthenticationTicket(anonymous, Scheme.Name)));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValue))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header."));

        if (!AuthenticationHeaderValue.TryParse(authHeaderValue, out var authHeader)
            || !string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || authHeader.Parameter is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        string[] credentials;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
            credentials = decoded.Split(':', 2);
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Credentials are not valid Base64."));
        }

        if (credentials.Length != 2
            || !FixedTimeEquals(credentials[0], _settings.Username)
            || !FixedTimeEquals(credentials[1], _settings.Password))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, credentials[0])], Scheme.Name));
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"OutlookSync\", charset=\"UTF-8\"";
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing-based attacks.
    /// </summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
