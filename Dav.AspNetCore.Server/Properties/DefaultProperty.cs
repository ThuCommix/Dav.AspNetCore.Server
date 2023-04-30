using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Properties.Primitives;
using Dav.AspNetCore.Server.Stores;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Properties;

public static class DefaultProperty
{
    public static TypedProperty<T, DateTime> CreationDate<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<DateTime>>>? getValueAsync = null,
        Func<HttpContext, T, DateTime, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavDate<T>(
            XmlNames.CreationDate,
            new Iso8601DateConverter())
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, string> DisplayName<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<string>>>? getValueAsync = null,
        Func<HttpContext, T, string, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavString<T>(XmlNames.DisplayName)
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, string> ContentLanguage<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<string>>>? getValueAsync = null,
        Func<HttpContext, T, string, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavString<T>(XmlNames.GetContentLanguage)
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, long> ContentLength<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<long>>>? getValueAsync = null,
        Func<HttpContext, T, long, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavInt64<T>(XmlNames.GetContentLength)
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, string> ContentType<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<string>>>? getValueAsync = null,
        Func<HttpContext, T, string, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavString<T>(XmlNames.GetContentType)
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, string> Etag<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<string>>>? getValueAsync = null,
        Func<HttpContext, T, string, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavString<T>(XmlNames.GetEtag)
        {
            IsExpensive = true,
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, DateTime> LastModified<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<DateTime>>>? getValueAsync = null,
        Func<HttpContext, T, DateTime, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavDate<T>(
            XmlNames.GetLastModified,
            new Rfc1123DateConverter())
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, XElement?> ResourceType<T>(
        Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult<XElement?>>>? getValueAsync = null,
        Func<HttpContext, T, XElement?, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync = null)
        where T : IStoreItem
    {
        return new DavXElement<T>(XmlNames.ResourceType)
        {
            GetValueAsync = getValueAsync,
            SetValueAsync = setValueAsync
        };
    }
    
    public static TypedProperty<T, IEnumerable<XElement>?> SupportedLock<T>()
        where T : IStoreItem
    {
        async ValueTask<PropertyResult<IEnumerable<XElement>?>> GetValueAsync(HttpContext context, T item, CancellationToken cancellationToken)
        {
            var supportedLocks = await item.LockManager.GetSupportedLocksAsync(item, cancellationToken);

            var lockEntries = new List<XElement>();
            foreach (var supportedLock in supportedLocks)
            {
                var lockScope = new XElement(XmlNames.LockScope, new XElement(XName.Get(supportedLock.ToString().ToLower(), XmlNames.Namespace)));
                var lockType = new XElement(XmlNames.LockType, new XElement(XmlNames.Write));
                var lockEntry = new XElement(XmlNames.LockEntry, lockScope, lockType);

                lockEntries.Add(lockEntry);
            }

            return PropertyResult.Success<IEnumerable<XElement>?>(lockEntries);
        }

        return new DavXElementArray<T>(XmlNames.SupportedLock)
        {
            GetValueAsync = GetValueAsync
        };
    }
    
    public static TypedProperty<T, IEnumerable<XElement>?> LockDiscovery<T>()
        where T : IStoreItem
    {
        async ValueTask<PropertyResult<IEnumerable<XElement>?>> GetValueAsync(HttpContext context, T item, CancellationToken cancellationToken)
        {
            var currentLocks = await item.LockManager.GetLocksAsync(item.Uri, cancellationToken);

            var activeLocks = new List<XElement>();
            foreach (var resourceLock in currentLocks)
            {
                var activelock = new XElement(XmlNames.ActiveLock);
                activelock.Add(new XElement(XmlNames.LockType, new XElement(XmlNames.Write)));
            
                switch (resourceLock.LockType)
                {
                    case LockType.Exclusive:
                        activelock.Add(new XElement(XmlNames.LockScope, new XElement(XmlNames.Exclusive)));
                        break;
                    case LockType.Shared:
                        activelock.Add(new XElement(XmlNames.LockScope, new XElement(XmlNames.Shared)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var timeoutValue = "Infinite";
                if (resourceLock.Timeout != TimeSpan.Zero)
                    timeoutValue = $"Second-{(resourceLock.ExpireDate - DateTime.UtcNow).TotalSeconds:F0}";
                
                activelock.Add(new XElement(XmlNames.Depth, resourceLock.Recursive ? "infinity" : "0"));
                activelock.Add(new XElement(XmlNames.Owner, new XElement(XmlNames.Href, resourceLock.Owner)));
                activelock.Add(new XElement(XmlNames.Timeout, timeoutValue));
                activelock.Add(new XElement(XmlNames.LockToken, new XElement(XmlNames.Href, $"urn:uuid:{resourceLock.Id:D}")));
                activelock.Add(new XElement(XmlNames.LockRoot, new XElement(XmlNames.Href, $"{context.Request.PathBase}{resourceLock.Uri.AbsolutePath}")));
                
                activeLocks.Add(activelock);
            }
            
            return PropertyResult.Success<IEnumerable<XElement>?>(activeLocks);
        }

        return new DavXElementArray<T>(XmlNames.LockDiscovery)
        {
            GetValueAsync = GetValueAsync
        };
    }
}