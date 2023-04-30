using System.Xml.Linq;

namespace Dav.AspNetCore.Server;

internal static class XmlNames
{
    public static readonly string Namespace = "DAV:";
    
    public static readonly XName PropertyFind = XName.Get("propfind", Namespace);
    
    public static readonly XName AllProperties = XName.Get("allprop", Namespace);

    public static readonly XName PropertyName = XName.Get("propname", Namespace);
    
    public static readonly XName Include = XName.Get("include", Namespace);
    
    public static readonly XName MultiStatus = XName.Get("multistatus", Namespace);
    
    public static readonly XName Response = XName.Get("response", Namespace);
    
    public static readonly XName Href = XName.Get("href", Namespace);
    
    public static readonly XName PropertyStatus = XName.Get("propstat", Namespace);
    
    public static readonly XName Property = XName.Get("prop", Namespace);
    
    public static readonly XName Status = XName.Get("status", Namespace);
    
    public static readonly XName PropertyUpdate = XName.Get("propertyupdate", Namespace);
    
    public static readonly XName Set = XName.Get("set", Namespace);
    
    public static readonly XName Remove = XName.Get("remove", Namespace);
    
    public static readonly XName CreationDate = XName.Get("creationdate", Namespace);
    
    public static readonly XName DisplayName = XName.Get("displayname", Namespace);
    
    public static readonly XName GetContentLanguage = XName.Get("getcontentlanguage", Namespace);
    
    public static readonly XName GetContentLength = XName.Get("getcontentlength", Namespace);
    
    public static readonly XName GetContentType = XName.Get("getcontenttype", Namespace);
    
    public static readonly XName GetEtag = XName.Get("getetag", Namespace);
    
    public static readonly XName GetLastModified = XName.Get("getlastmodified", Namespace);
    
    public static readonly XName ResourceType = XName.Get("resourcetype", Namespace);
    
    public static readonly XName LockInfo = XName.Get("lockinfo", Namespace);
    
    public static readonly XName LockScope = XName.Get("lockscope", Namespace);
    
    public static readonly XName LockType = XName.Get("locktype", Namespace);
    
    public static readonly XName Owner = XName.Get("owner", Namespace);
    
    public static readonly XName Exclusive = XName.Get("exclusive", Namespace);
    
    public static readonly XName Shared = XName.Get("shared", Namespace);
    
    public static readonly XName Write = XName.Get("write", Namespace);
    
    public static readonly XName LockDiscovery = XName.Get("lockdiscovery", Namespace);
    
    public static readonly XName ActiveLock = XName.Get("activelock", Namespace);
    
    public static readonly XName Depth = XName.Get("depth", Namespace);
    
    public static readonly XName Timeout = XName.Get("timeout", Namespace);
    
    public static readonly XName LockToken = XName.Get("locktoken", Namespace);
    
    public static readonly XName LockRoot = XName.Get("lockroot", Namespace);
    
    public static readonly XName LockTokenSubmitted = XName.Get("lock-token-submitted", Namespace);
    
    public static readonly XName Error = XName.Get("error", Namespace);
    
    public static readonly XName LockEntry = XName.Get("lockentry", Namespace);
    
    public static readonly XName SupportedLock = XName.Get("supportedlock", Namespace);

    public static readonly XName Collection = XName.Get("collection", "DAV:");
}