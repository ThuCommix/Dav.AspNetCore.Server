namespace Dav.AspNetCore.Server.Store.Properties;

public record PropertyResult(DavStatusCode StatusCode, object? Value = null)
{
    public bool IsSuccess => StatusCode == DavStatusCode.Ok;
}