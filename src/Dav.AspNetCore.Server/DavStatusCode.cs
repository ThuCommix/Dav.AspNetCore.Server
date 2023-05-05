using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server;

public enum DavStatusCode
{
    [DavStatusCode("OK")]
    Ok = StatusCodes.Status200OK,
    
    [DavStatusCode("Created")]
    Created = StatusCodes.Status201Created,
    
    [DavStatusCode("No Content")]
    NoContent = StatusCodes.Status204NoContent,
    
    [DavStatusCode("Partial Content")]
    PartialContent = StatusCodes.Status206PartialContent,
    
    [DavStatusCode("Multi-Status")]
    MultiStatus = StatusCodes.Status207MultiStatus,
    
    [DavStatusCode("Not Modified")]
    NotModified = StatusCodes.Status304NotModified,

    [DavStatusCode("Bad Request")]
    BadRequest = StatusCodes.Status400BadRequest,
    
    [DavStatusCode("Unauthorized")]
    Unauthorized = StatusCodes.Status401Unauthorized,
    
    [DavStatusCode("Forbidden")]
    Forbidden = StatusCodes.Status403Forbidden,
    
    [DavStatusCode("Not Found")]
    NotFound = StatusCodes.Status404NotFound,
    
    [DavStatusCode("Method Not Allowed")]
    NotAllowed = StatusCodes.Status405MethodNotAllowed,
    
    [DavStatusCode("Conflict")]
    Conflict = StatusCodes.Status409Conflict,
    
    [DavStatusCode("Precondition Failed")]
    PreconditionFailed = StatusCodes.Status412PreconditionFailed,
    
    [DavStatusCode("Locked")]
    Locked = StatusCodes.Status423Locked,
    
    [DavStatusCode("Failed Dependency")]
    FailedDependency = StatusCodes.Status424FailedDependency,
    
    [DavStatusCode("Internal Server Error")]
    InternalServerError = StatusCodes.Status500InternalServerError,
    
    [DavStatusCode("Insufficient Storage")]
    InsufficientStorage = StatusCodes.Status507InsufficientStorage,
}

internal static class DavStatusCodeExtensions
{
    private static readonly Type EnumType = typeof(DavStatusCode);
    public static string? GetDisplayName(this DavStatusCode statusCode)
    {
        var memberInfo = EnumType.GetMember(statusCode.ToString());
        var attribute = memberInfo[0].GetCustomAttribute<DavStatusCodeAttribute>(false);
        return attribute?.DisplayName;
    }
}