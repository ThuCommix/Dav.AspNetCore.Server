using System.Xml.Linq;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavXElement<T> : TypedProperty<T, XElement?> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavXElementArray{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    public DavXElement(XName name) 
        : base(name)
    {
        Converter = new XElementConverter();
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected override IConverter<XElement?> Converter { get; }
}