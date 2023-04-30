using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavDate<T> : TypedProperty<T, DateTime> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavInt32{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="converter">The date converter.</param>
    public DavDate(XName name, IConverter<DateTime> converter)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(converter, nameof(converter));
        
        Converter = converter;
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected override IConverter<DateTime> Converter { get; }
}