using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dav.AspNetCore.Server.Store.Properties;

public delegate ValueTask PropertyReadCallback(PropertyReadContext context, CancellationToken cancellationToken = default);
public delegate ValueTask PropertyChangeCallback(PropertyChangeContext context, CancellationToken cancellationToken = default);

public class Property
{
    internal static readonly Dictionary<Type, Dictionary<XName, Property>> Registrations = new();
    internal readonly PropertyReadCallback? ReadCallback;
    internal readonly PropertyChangeCallback? ChangeCallback;

    /// <summary>
    /// Initializes a new <see cref="Property"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="read">The read callback.</param>
    /// <param name="change">The change callback.</param>
    internal Property(
        XName name,
        PropertyMetadata metadata,
        PropertyReadCallback? read = null,
        PropertyChangeCallback? change = null)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));

        Name = name;
        Metadata = metadata;
        ReadCallback = read;
        ChangeCallback = change;
    }
    
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public XName Name { get; }
    
    /// <summary>
    /// Gets the property metadata.
    /// </summary>
    public PropertyMetadata Metadata { get; }

    /// <summary>
    /// Registers a property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="read">The property read callback.</param>
    /// <param name="change">The property changed callback.</param>
    /// <param name="metadata">The property metadata.</param>
    /// <typeparam name="TOwner">The owner type on which the property gets registered on.</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Property RegisterProperty<TOwner>(
        XName name,
        PropertyReadCallback? read = null,
        PropertyChangeCallback? change = null,
        PropertyMetadata? metadata = null) where TOwner : class, IStoreItem
    {
        var ownerType = typeof(TOwner);
        var property = new Property(
            name,
            metadata ?? PropertyMetadata.Default,
            read,
            change);

        if (!Registrations.TryGetValue(ownerType, out var propertyMap))
        {
            propertyMap = new Dictionary<XName, Property>();
            Registrations.Add(ownerType, propertyMap);
        }
        
        if (propertyMap.ContainsKey(name))
            throw new InvalidOperationException($"The property: {name} was already registered for: {ownerType.FullName}.");

        propertyMap[name] = property;
        return property;
    }

    /// <summary>
    /// Registers the supported lock property.
    /// </summary>
    /// <typeparam name="TOwner">The owner type on which the property gets registered on.</typeparam>
    /// <returns></returns>
    public static Property RegisterSupportedLockProperty<TOwner>() where TOwner : class, IStoreItem
    {
        return RegisterProperty<TOwner>(
            XmlNames.SupportedLock,
            read: async (context, cancellationToken) =>
            {
                var lockManager = context.Services.GetRequiredService<ILockManager>();
                var supportedLocks = await lockManager.GetSupportedLocksAsync(context.Item, cancellationToken);

                var lockEntries = new List<XElement>();
                foreach (var supportedLock in supportedLocks)
                {
                    var lockScope = new XElement(XmlNames.LockScope, new XElement(XName.Get(supportedLock.ToString().ToLower(), XmlNames.Namespace)));
                    var lockType = new XElement(XmlNames.LockType, new XElement(XmlNames.Write));
                    var lockEntry = new XElement(XmlNames.LockEntry, lockScope, lockType);

                    lockEntries.Add(lockEntry);
                }

                context.SetResult(lockEntries);
            },
            metadata: new PropertyMetadata(Computed: true));
    }

    /// <summary>
    /// Registers the lock discovery property.
    /// </summary>
    /// <typeparam name="TOwner">The owner type on which the property gets registered on.</typeparam>
    /// <returns></returns>
    public static Property RegisterLockDiscoveryProperty<TOwner>() where TOwner : class, IStoreItem
    {
        return RegisterProperty<TOwner>(
            XmlNames.LockDiscovery,
            read: async (context, cancellationToken) =>
            {
                var httpContextAccessor = context.Services.GetService<IHttpContextAccessor>();
                var lockManager = context.Services.GetRequiredService<ILockManager>();
                var currentLocks = await lockManager
                    .GetLocksAsync(context.Item.Uri, cancellationToken);

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
                    activelock.Add(new XElement(XmlNames.LockRoot, new XElement(XmlNames.Href, $"{httpContextAccessor?.HttpContext?.Request.PathBase}{resourceLock.Uri.AbsolutePath}")));

                    activeLocks.Add(activelock);
                }

                context.SetResult(activeLocks);
            },
            metadata: new PropertyMetadata(Computed: true));
    }
}