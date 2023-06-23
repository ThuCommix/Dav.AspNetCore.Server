using System.Xml.Linq;
using Dav.AspNetCore.Server.Http.Headers;
using Dav.AspNetCore.Server.Locks;

namespace Dav.AspNetCore.Server.Handlers;

internal class LockHandler : RequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task HandleRequestAsync(CancellationToken cancellationToken = default)
    {
        var requestPath = new ResourcePath(Context.Request.Path);
        if (WebDavHeaders.Timeouts.Count == 0)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        var timeout = WebDavHeaders.Timeouts
            .OrderByDescending(x => x.TotalSeconds)
            .First(x => x <= (Options.MaxLockTimeout ?? TimeSpan.MaxValue));

        if (WebDavHeaders.Timeouts.Any(x => x == TimeSpan.Zero) && Options.MaxLockTimeout == null)
            timeout = TimeSpan.Zero;

        var depth = WebDavHeaders.Depth ?? Depth.Infinity;
        if (depth == Depth.One)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        LockResult? result = null;

        // check if a lock should be refreshed
        if (string.IsNullOrWhiteSpace(Context.Request.ContentType) &&
            WebDavHeaders.If.Count > 0 &&
            (Context.Request.ContentLength == null ||
             Context.Request.ContentLength == 0))
        {
            var condition = WebDavHeaders.If
                .FirstOrDefault(x => x.Uri == null || x.Uri.AbsolutePath == requestPath);

            if (condition == null)
            {
                Context.SetResult(DavStatusCode.PreconditionFailed);
                return;
            }
            
            var activeLocks = await LockManager.GetLocksAsync(
                requestPath,
                cancellationToken);

            var activeLock = activeLocks
                .FirstOrDefault(x => x.Path == requestPath && condition.Tokens.Any(z => x.Id.AbsoluteUri == z.Value));
            
            if (activeLock == null)
            {
                Context.SetResult(DavStatusCode.PreconditionFailed);
                return;
            }

            result = await LockManager.RefreshLockAsync(
                requestPath,
                activeLock.Id,
                timeout,
                cancellationToken);
        } 
        else if (Context.Request.ContentType != null &&
                 (Context.Request.ContentType.Contains("application/xml") ||
                  Context.Request.ContentType.Contains("text/xml")) &&
                 Context.Request.ContentLength > 0)
        {
            var requestDocument = await Context.ReadDocumentAsync(cancellationToken);
            if (requestDocument == null)
            {
                Context.SetResult(DavStatusCode.BadRequest);
                return;
            }

            var lockInfo = requestDocument.Element(XmlNames.LockInfo);
            var lockScope = lockInfo?.Element(XmlNames.LockScope);
            var lockType = lockInfo?.Element(XmlNames.LockType);
            var owner = lockInfo?.Element(XmlNames.Owner)?.FirstNode as XElement;

            var exclusive = lockScope?.Element(XmlNames.Exclusive);
            var shared = lockScope?.Element(XmlNames.Shared);
            var write = lockType?.Element(XmlNames.Write);

            if (lockScope == null ||
                lockType == null ||
                owner == null ||
                write == null ||
                (exclusive == null && shared == null))
            {
                Context.SetResult(DavStatusCode.BadRequest);
                return;
            }

            result = await LockManager.LockAsync(
                requestPath,
                exclusive != null ? LockType.Exclusive : LockType.Shared,
                owner,
                depth == Depth.Infinity,
                timeout,
                cancellationToken);

            if (result.StatusCode == DavStatusCode.Locked)
            {
                await Context.SendLockedAsync(requestPath, cancellationToken);
                return;
            }
        }

        if (result == null)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        if (result.ResourceLock == null)
        {
            Context.SetResult(result.StatusCode);
            return;
        }

        var document = BuildDocument(result.ResourceLock);
        await Context.WriteDocumentAsync(DavStatusCode.Ok, document, cancellationToken);
    }

    /// <summary>
    /// Checks if the given resource path is locked async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the resource path is locked, otherwise false.</returns>
    protected override async ValueTask<bool> CheckLockedAsync(ResourcePath path, CancellationToken cancellationToken = default)
    {
        // the default behavior would block the request when a lock exists
        // how ever in the lock handler we can still proceed if all of them are "shared"
        // the lock manager makes sure, we only issue "shared" locks
        var activeLocks = await LockManager.GetLocksAsync(path, cancellationToken);
        return activeLocks.Any(x => x.LockType == LockType.Exclusive);
    }

    private XDocument BuildDocument(ResourceLock resourceLock)
    {
        var lockType = new XElement(XmlNames.LockType, new XElement(XmlNames.Write));
        var lockScope = new XElement(XmlNames.LockScope,
            resourceLock.LockType == LockType.Exclusive
                ? new XElement(XmlNames.Exclusive)
                : new XElement(XmlNames.Shared));

        var depth = new XElement(XmlNames.Depth, resourceLock.Recursive ? "infinity" : "0");
        var owner = new XElement(XmlNames.Owner, resourceLock.Owner);
        var timeout = new XElement(XmlNames.Timeout, resourceLock.Timeout == TimeSpan.Zero
            ? "Infinite"
            : $"Second-{resourceLock.Timeout.TotalSeconds:F0}");

        var lockToken = new XElement(XmlNames.LockToken, new XElement(XmlNames.Href, resourceLock.Id.AbsoluteUri));
        var lockRoot = new XElement(XmlNames.LockRoot, new XElement(XmlNames.Href, $"{Context.Request.PathBase}{resourceLock.Path}"));
        var activeLock = new XElement(XmlNames.ActiveLock, lockType, lockScope, depth, owner, timeout, lockToken, lockRoot);
        var lockDiscovery = new XElement(XmlNames.LockDiscovery, activeLock);
        var prop = new XElement(XmlNames.Property, lockDiscovery);
        
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            prop);
    }
}