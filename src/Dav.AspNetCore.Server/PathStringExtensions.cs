using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server;

internal static class PathStringExtensions
{
    public static Uri ToUri(this PathString path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new Uri("/");

        var uri = Uri.UnescapeDataString(path.ToUriComponent().TrimEnd('/'));
        if (string.IsNullOrWhiteSpace(uri))
            return new Uri("/");

        return new Uri(uri);
    }
}