using System.Xml.Linq;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavXElementArray<T> : TypedProperty<T, IEnumerable<XElement>?> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavXElementArray{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    public DavXElementArray(XName name) 
        : base(name)
    {
        Converter = new XElementArrayConverter();
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected override IConverter<IEnumerable<XElement>?> Converter { get; }
}