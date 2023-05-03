using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dav.AspNetCore.Server.Authentication;

internal class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.NoResult();

        var authorizationHeader = Request.Headers["Authorization"].ToString();
        if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var credentialsBase64 = Encoding.UTF8.GetString(
            Convert.FromBase64String(authorizationHeader.Replace("Basic ", "", StringComparison.OrdinalIgnoreCase)));
        
        var credentialParts = credentialsBase64.Split(new[] { ':' }, 2);
        
        if (credentialParts.Length != 2)
            return AuthenticateResult.Fail("Invalid Authorization header format");

        var authenticatingContext = new BasicAuthenticatingContext(
            Context,
            Options,
            Scheme,
            credentialParts[0], 
            credentialParts[1]);

        if (Options.Events.OnAuthenticating == null)
            return AuthenticateResult.NoResult();

        var authenticated = await Options.Events.OnAuthenticating(authenticatingContext, Context.RequestAborted);
        if (!authenticated)
            return AuthenticateResult.NoResult();

        var identity = new Identity(
            BasicAuthenticationDefaults.AuthenticationScheme,
            true,
            authenticatingContext.UserName);
        
        var claimsIdentity = new ClaimsIdentity(identity);
        var authenticatedContext = new AuthenticatedContext<BasicAuthenticationSchemeOptions>(
            Context,
            Options,
            Scheme,
            claimsIdentity);
        
        if (Options.Events.OnAuthenticated != null)
            await Options.Events.OnAuthenticated(authenticatedContext, Context.RequestAborted);

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }
    
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (string.IsNullOrWhiteSpace(Options.Realm))
        {
            Response.Headers["WWW-Authenticate"] = "Basic charset=\"UTF-8\"";
        }
        else
        {
            Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{Options.Realm}\", charset=\"UTF-8\"";
        }
        return base.HandleChallengeAsync(properties);
    }

    private record Identity(string? AuthenticationType, bool IsAuthenticated, string? Name) : IIdentity;
}