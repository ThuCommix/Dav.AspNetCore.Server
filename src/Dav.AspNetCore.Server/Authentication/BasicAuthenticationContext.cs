namespace Dav.AspNetCore.Server.Authentication;

/// <summary>
/// Initializes a new <see cref="BasicAuthenticationContext"/> class.
/// </summary>
/// <param name="UserName">The user name.</param>
/// <param name="Password">The password.</param>
public record BasicAuthenticationContext(string UserName, string Password);