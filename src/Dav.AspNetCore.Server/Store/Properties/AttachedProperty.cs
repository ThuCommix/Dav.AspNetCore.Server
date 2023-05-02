using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store.Properties.Converters;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Store.Properties;

/// <summary>
/// Represents a delegate to set a property value.
/// </summary>
/// <typeparam name="TOwner">The property owner type.</typeparam>
/// <typeparam name="TType">The property type.</typeparam>
public delegate ValueTask<DavStatusCode> SetPropertyDelegate<in TOwner, in TType>(
    HttpContext context,
    TOwner item,
    TType typedValue,
    CancellationToken cancellationToken) where TOwner : IStoreItem;

/// <summary>
/// Represents a delegate to retrieve a property value.
/// </summary>
/// <typeparam name="TOwner">The property owner type.</typeparam>
public delegate ValueTask<PropertyResult> GetPropertyDelegate<in TOwner>(
    HttpContext context,
    TOwner item,
    CancellationToken cancellationToken) where TOwner : IStoreItem;

/// <summary>
/// Initializes a new <see cref="AttachedProperty{T}"/> class.
/// </summary>
public abstract class AttachedProperty
{
    /// <summary>
    /// Initializes a new <see cref="AttachedProperty"/> class.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    protected AttachedProperty(PropertyMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));
        Metadata = metadata;
    }
    
    /// <summary>
    /// Gets the metadata.
    /// </summary>
    public PropertyMetadata Metadata { get; }
    
    /// <summary>
    /// Gets the value setter.
    /// </summary>
    public SetPropertyDelegate<IStoreItem, object?>? SetValueAsync { get; protected set; }

    /// <summary>
    /// Gets the value getter.
    /// </summary>
    public GetPropertyDelegate<IStoreItem>? GetValueAsync { get; protected set; }
    
    /// <summary>
    /// Creates a new attached property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="converter">The converter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="TOwner">The property owner type.</typeparam>
    /// <typeparam name="TType">The property type.</typeparam>
    /// <returns>The attached property.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static AttachedProperty<TOwner, TType> CreateAttached<TOwner, TType>(
        XName name,
        GetPropertyDelegate<TOwner>? getter = null,
        SetPropertyDelegate<TOwner, TType>? setter = null,
        IConverter<TType>? converter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where TOwner : IStoreItem
    {
        if ((setter != null || getter != null) && converter == null)
            throw new InvalidOperationException("If the property has a getter or setter, the converter is required.");
        
        return new AttachedPropertyImpl<TOwner, TType>(
            new PropertyMetadata(name, isExpensive, isProtected, isComputed),
            converter)
        {
            GetValueAsync = getter,
            SetValueAsync = setter
        };
    }

    /// <summary>
    /// Creates the creation date property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty<T> CreationDate<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, DateTime>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.CreationDate,
            getter,
            setter,
            new Iso8601DateConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the display name property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty<T> DisplayName<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, string>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.DisplayName,
            getter,
            setter,
            new StringConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the content language property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty<T> ContentLanguage<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, string>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.GetContentLanguage,
            getter,
            setter,
            new StringConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the content length property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty<T> ContentLength<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, long>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.GetContentLength,
            getter,
            setter,
            new Int64Converter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the content type property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty<T> ContentType<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, string>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.GetContentType,
            getter,
            setter,
            new StringConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the entity tag property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty<T> Etag<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, string>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.GetEtag,
            getter,
            setter,
            new StringConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the last modified property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty LastModified<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, DateTime>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.GetLastModified,
            getter,
            setter,
            new Rfc1123DateConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the resource type property.
    /// </summary>
    /// <param name="setter">The value setter.</param>
    /// <param name="getter">The value getter.</param>
    /// <param name="isExpensive">A value indicating whether the property is expensive to retrieve.</param>
    /// <param name="isProtected">A value indicating whether the property is protected.</param>
    /// <param name="isComputed">A value indicating whether the property is calculated.</param>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty ResourceType<T>(
        GetPropertyDelegate<T>? getter = null,
        SetPropertyDelegate<T, XElement?>? setter = null,
        bool isExpensive = false,
        bool isProtected = false,
        bool isComputed = false)
        where T : IStoreItem
    {
        return CreateAttached(
            XmlNames.ResourceType,
            getter,
            setter,
            new XElementConverter(),
            isExpensive,
            isProtected,
            isComputed);
    }
    
    /// <summary>
    /// Creates the supported lock property.
    /// </summary>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty SupportedLock<T>()
        where T : IStoreItem
    {
        async ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken) where TOwner : IStoreItem
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

            return PropertyResult.Success(lockEntries);
        }
        
        return CreateAttached<T, IEnumerable<XElement>>(
            XmlNames.SupportedLock,
            GetValueAsync,
            null,
            new XElementArrayConverter(),
            false,
            true,
            true);
    }
    
    /// <summary>
    /// Creates the lock discovery property.
    /// </summary>
    /// <typeparam name="T">The property owner type.</typeparam>
    /// <returns>The attached property.</returns>
    public static AttachedProperty LockDiscovery<T>()
        where T : IStoreItem
    {
        async ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken) where TOwner : IStoreItem
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

            return PropertyResult.Success(activeLocks);
        }
        
        return CreateAttached<T, IEnumerable<XElement>>(
            XmlNames.LockDiscovery,
            GetValueAsync,
            null,
            new XElementArrayConverter(),
            false,
            true,
            true);
    }
    
    private class AttachedPropertyImpl<TOwner, TType> : AttachedProperty<TOwner, TType> where TOwner : IStoreItem
    {
        /// <summary>
        /// Initializes a new <see cref="AttachedProperty{THost,TType}"/> class.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="converter">The converter.</param>
        public AttachedPropertyImpl(
            PropertyMetadata metadata,
            IConverter<TType>? converter = null) 
            : base(metadata)
        {
            Converter = converter ?? new NotSupportedConverter();
        }

        /// <summary>
        /// Gets the converter.
        /// </summary>
        protected override IConverter<TType> Converter { get; }

        private class NotSupportedConverter : IConverter<TType>
        {
            public object Convert(TType value)
            {
                throw new NotSupportedException();
            }

            public TType ConvertBack(object? value)
            {
                throw new NotSupportedException();
            }
        }
    }
}

/// <summary>
/// Initializes a new <see cref="AttachedProperty{T}"/> class.
/// </summary>
/// <typeparam name="T">The store item type.</typeparam>
public abstract class AttachedProperty<T> : AttachedProperty where T : IStoreItem
{
    private SetPropertyDelegate<T, object?>? setValueAsync;
    private GetPropertyDelegate<T>? getValueAsync;
    
    /// <summary>
    /// Initializes a new <see cref="AttachedProperty{T}"/> class.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    protected AttachedProperty(PropertyMetadata metadata)
        : base(metadata)
    {
    }

    /// <summary>
    /// Gets the value setter.
    /// </summary>
    public new SetPropertyDelegate<T, object?>? SetValueAsync
    {
        get => setValueAsync;
        set
        {
            setValueAsync = value;
            if (setValueAsync == null)
            {
                base.SetValueAsync = null;
                return;
            }

            base.SetValueAsync = (context, item, value, cancellationToken) => setValueAsync(context, (T)item, value, cancellationToken);
        }
    }

    /// <summary>
    /// Gets the value getter.
    /// </summary>
    public new GetPropertyDelegate<T>? GetValueAsync
    {
        get => getValueAsync;
        set
        {
            getValueAsync = value;
            if (getValueAsync == null)
            {
                base.GetValueAsync = null;
                return;
            }
            
            base.GetValueAsync = async (context, item, cancellationToken) =>
            {
                var result = await getValueAsync(context, (T)item, cancellationToken).ConfigureAwait(false);
                return result.IsSuccess 
                    ? PropertyResult.Success(result.Value)
                    : PropertyResult.Fail(result.StatusCode);
            };   
        }
    }
}

/// <summary>
/// Initializes a new <see cref="AttachedProperty{THost,TType}"/> class.
/// </summary>
/// <typeparam name="TOwner">The store item type.</typeparam>
/// <typeparam name="TType">The property data type.</typeparam>
public abstract class AttachedProperty<TOwner, TType> : AttachedProperty<TOwner> where TOwner : IStoreItem
{
    private SetPropertyDelegate<TOwner, TType>? setValueAsync;
    private GetPropertyDelegate<TOwner>? getValueAsync;
    
    /// <summary>
    /// Initializes a new <see cref="AttachedProperty{THost,TType}"/> class.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    protected AttachedProperty(PropertyMetadata metadata) 
        : base(metadata)
    {
    }
    
    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected abstract IConverter<TType> Converter { get; }
    
    /// <summary>
    /// Gets the value setter.
    /// </summary>
    public new SetPropertyDelegate<TOwner, TType>? SetValueAsync
    {
        get => setValueAsync;
        set
        {
            setValueAsync = value;
            if (setValueAsync == null)
            {
                base.SetValueAsync = null;
                return;
            }

            base.SetValueAsync = (context, item, value, cancellationToken) =>
            {
                var typedValue = Converter.ConvertBack(value);
                return setValueAsync(context, item, typedValue, cancellationToken);
            };
        }
    }

    /// <summary>
    /// Gets the value getter.
    /// </summary>
    public new GetPropertyDelegate<TOwner>? GetValueAsync
    {
        get => getValueAsync;
        set
        {
            getValueAsync = value;
            if (getValueAsync == null)
            {
                base.GetValueAsync = null;
                return;
            }
            
            base.GetValueAsync = async (context, item, cancellationToken) =>
            {
                var result = await getValueAsync(context, item, cancellationToken).ConfigureAwait(false);
                return result.IsSuccess 
                    ? PropertyResult.Success(Converter.Convert((TType)result.Value!))
                    : PropertyResult.Fail(result.StatusCode);
            };   
        }
    }
}