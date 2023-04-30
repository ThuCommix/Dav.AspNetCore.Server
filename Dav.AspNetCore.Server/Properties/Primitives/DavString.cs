using System.Xml.Linq;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavString<T> : TypedProperty<T, string> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavString{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    public DavString(XName name) 
        : base(name)
    {
        Converter = new StringConverter();
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected override IConverter<string> Converter { get; }
}