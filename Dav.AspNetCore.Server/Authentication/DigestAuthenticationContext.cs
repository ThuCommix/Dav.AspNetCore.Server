namespace Dav.AspNetCore.Server.Authentication;

/// <summary>
/// Initializes a new <see cref="DigestAuthenticationContext"/> clas.s.
/// </summary>
/// <param name="UserName">The user name.</param>
public record DigestAuthenticationContext(string UserName);