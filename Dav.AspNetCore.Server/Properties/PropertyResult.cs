namespace Dav.AspNetCore.Server.Properties;

/// <summary>
/// Initializes a new <see cref="PropertyResult"/> class.
/// </summary>
/// <param name="StatusCode">The status code.</param>
/// <param name="Value">The value.</param>
public record PropertyResult(DavStatusCode StatusCode, object? Value)
{
    /// <summary>
    /// Creates an errored result.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    /// <returns>The property result.</returns>
    public static PropertyResult Fail(DavStatusCode statusCode) => new(statusCode, null);
    
    /// <summary>
    /// Creates a succeeded result.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <returns>The property result.</returns>
    public static PropertyResult Success(object? value) => new(DavStatusCode.Ok, value);
    
    /// <summary>
    /// Creates a succeeded result.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <returns>The property result.</returns>
    public static PropertyResult<T> Success<T>(T value) => new(DavStatusCode.Ok, value);

    /// <summary>
    /// A value indicating whether the property result is successful.
    /// </summary>
    public bool IsSuccess => StatusCode == DavStatusCode.Ok;
}

/// <summary>
/// Initializes a new <see cref="PropertyResult{T}"/> class.
/// </summary>
/// <param name="StatusCode">The status code.</param>
/// <param name="TypedValue">The value.</param>
/// <typeparam name="T">The result type.</typeparam>
public record PropertyResult<T>(
        DavStatusCode StatusCode,
        T TypedValue)
    : PropertyResult(StatusCode, TypedValue);