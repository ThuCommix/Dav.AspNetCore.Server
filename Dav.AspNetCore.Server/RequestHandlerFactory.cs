using System.Diagnostics.CodeAnalysis;
using Dav.AspNetCore.Server.Handlers;
using Dav.AspNetCore.Server.Http;

namespace Dav.AspNetCore.Server;

internal static class RequestHandlerFactory
{
    private static readonly Dictionary<string, Func<IRequestHandler>> Mappings = new()
    {
        { WebDavMethods.Options, () => new OptionsHandler() },
        { WebDavMethods.MkCol, () => new MkColHandler() },
        { WebDavMethods.PropFind, () => new PropFindHandler() },
        { WebDavMethods.PropPatch, () => new PropPatchHandler() },
        { WebDavMethods.Head, () => new GetHandler() },
        { WebDavMethods.Get, () => new GetHandler() },
        { WebDavMethods.Put, () => new PutHandler() },
        { WebDavMethods.Delete, () => new DeleteHandler() },
        { WebDavMethods.Lock, () => new LockHandler() },
        { WebDavMethods.Unlock, () => new UnlockHandler() },
        { WebDavMethods.Copy, () => new CopyHandler() },
        { WebDavMethods.Move, () => new MoveHandler() }
    };

    public static bool TryGetRequestHandler(string method, [NotNullWhen(true)] out IRequestHandler? requestHandler)
    {
        if (Mappings.TryGetValue(method, out var factory))
        {
            requestHandler = factory();
            return true;
        }

        requestHandler = null;
        return false;
    }
}