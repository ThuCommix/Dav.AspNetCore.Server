using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dav.AspNetCore.Server.Authentication;

internal class DigestAuthenticationHandler : AuthenticationHandler<DigestAuthenticationSchemeOptions>
{
    private static readonly List<Guid> OpaqueIds = new();

    public DigestAuthenticationHandler(
        IOptionsMonitor<DigestAuthenticationSchemeOptions> options, 
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
        if (!authorizationHeader.StartsWith("Digest ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var parameters = authorizationHeader.Replace("Digest ", string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', StringSplitOptions.TrimEntries))
            .ToDictionary(x => x[0], x => x[1].Trim('"'));

        if (!parameters.TryGetValue("username", out var userName))
            return AuthenticateResult.NoResult();
        
        if (!parameters.TryGetValue("realm", out var realm))
            return AuthenticateResult.NoResult();
        
        if (!parameters.TryGetValue("nonce", out var nonce))
            return AuthenticateResult.NoResult();

        parameters.TryGetValue("nc", out var nonceCount);

        parameters.TryGetValue("cnonce", out var clientNonce);
        
        if (!parameters.TryGetValue("uri", out var uri))
            return AuthenticateResult.NoResult();

        parameters.TryGetValue("qop", out var qop);

        if (!parameters.TryGetValue("response", out var clientResponse))
            return AuthenticateResult.NoResult();
        
        if (!parameters.TryGetValue("opaque", out var opaque))
            return AuthenticateResult.NoResult();

        if (Options.Events.OnRequestPassword == null)
            return AuthenticateResult.NoResult();

        if (!Guid.TryParse(opaque, out var opaqueId))
            return AuthenticateResult.Fail("Opaque could not be parsed.");

        if (!OpaqueIds.Contains(opaqueId))
            return AuthenticateResult.NoResult();

        var digestContext = new DigestAuthenticationContext(userName);
        var password = await Options.Events.OnRequestPassword(digestContext, Context.RequestAborted);

        if (string.IsNullOrWhiteSpace(password))
            return AuthenticateResult.NoResult();

        var ha1 = ComputeMd5($"{userName}:{realm}:{password}");
        var ha2 = ComputeMd5($"{Context.Request.Method}:{uri}");

        string response;
        if (string.IsNullOrWhiteSpace(qop))
        {
            response = ComputeMd5($"{ha1}:{nonce}:{ha2}");
        }
        else if (qop.Equals("auth"))
        {
            response = ComputeMd5($"{ha1}:{nonce}:{nonceCount}:{clientNonce}:{qop}:{ha2}");
        }
        else
        {
            return AuthenticateResult.Fail("Unsupported quality of protection parameter.");
        }

        if (response != clientResponse)
            return AuthenticateResult.NoResult();
        
        var identity = new Identity(
            BasicAuthenticationDefaults.AuthenticationScheme,
            true,
            userName);

        var claimsIdentity = new ClaimsIdentity(identity);
        if (Options.Events.OnAuthenticated != null)
            await Options.Events.OnAuthenticated(digestContext, claimsIdentity, Context.RequestAborted);

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }
    
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Context.Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{Options.Realm ?? Context.Request.Host.ToString()}\", " +
                                                       "qop=\"auth\", " +
                                                       $"nonce=\"{GenerateNonce()}\", " +
                                                       $"opaque=\"{GenerateOpaque()}\", " +
                                                       "algorithm=\"MD5\"";
        return base.HandleChallengeAsync(properties);
    }

    private string GenerateOpaque()
    {
        var guid = Guid.NewGuid();
        OpaqueIds.Add(guid);

        return guid.ToString("N");
    }

    private string GenerateNonce() => Guid.NewGuid().ToString("N");

    private string ComputeMd5(string input)
    {
        using var algorithm = MD5.Create();
        var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
    
    private record Identity(string? AuthenticationType, bool IsAuthenticated, string? Name) : IIdentity;
}