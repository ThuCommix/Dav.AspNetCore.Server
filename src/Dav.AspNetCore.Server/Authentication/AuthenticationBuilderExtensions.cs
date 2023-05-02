using Microsoft.AspNetCore.Authentication;

namespace Dav.AspNetCore.Server.Authentication;

public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds basic authentication.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureAction">The configure action.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddBasic(
        this AuthenticationBuilder builder,
        Action<BasicAuthenticationSchemeOptions> configureAction,
        string? authenticationScheme = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configureAction, nameof(configureAction));

        builder.AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(
            authenticationScheme ?? BasicAuthenticationDefaults.AuthenticationScheme,
            BasicAuthenticationDefaults.AuthenticationScheme,
            configureAction);

        return builder;
    }
    
    /// <summary>
    /// Adds digest authentication.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureAction">The configure action.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddDigest(
        this AuthenticationBuilder builder,
        Action<DigestAuthenticationSchemeOptions> configureAction,
        string? authenticationScheme = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configureAction, nameof(configureAction));
        
        builder.AddScheme<DigestAuthenticationSchemeOptions, DigestAuthenticationHandler>(
            authenticationScheme ?? DigestAuthenticationDefaults.AuthenticationScheme,
            DigestAuthenticationDefaults.AuthenticationScheme,
            configureAction);

        return builder;
    }
}