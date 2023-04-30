using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Properties;

public abstract class TypedProperty<TResource, TType> : Property<TResource> where TResource : IStoreItem
{
    private Func<HttpContext, TResource, TType, CancellationToken, ValueTask<DavStatusCode>>? setValueAsync;
    private Func<HttpContext, TResource, CancellationToken, ValueTask<PropertyResult<TType>>>? getValueAsync;

    /// <summary>
    /// Initializes a new <see cref="TypedProperty{TResource,TType}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <typeparam name="TResource">The store item type.</typeparam>
    /// <typeparam name="TType">The property type.</typeparam>
    protected TypedProperty(XName name) 
        : base(name)
    {
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected abstract IConverter<TType> Converter { get; }

    /// <summary>
    /// Gets the value setter.
    /// </summary>
    public new Func<HttpContext, TResource, TType, CancellationToken, ValueTask<DavStatusCode>>? SetValueAsync
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
                var typedValue = Converter.FromXml(value);
                return setValueAsync(context, item, typedValue, cancellationToken);
            };
        }
    }

    /// <summary>
    /// Gets the value getter.
    /// </summary>
    public new Func<HttpContext, TResource, CancellationToken, ValueTask<PropertyResult<TType>>>? GetValueAsync
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
                    ? PropertyResult.Success(Converter.ToXml(result.TypedValue)) 
                    : PropertyResult.Fail(result.StatusCode);
            };   
        }
    }
}