using System.Xml.Linq;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavInt32<T> : TypedProperty<T, int> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavInt32{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    public DavInt32(XName name) 
        : base(name)
    {
        Converter = new Int32Converter();
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected override IConverter<int> Converter { get; }
}