using System.Xml.Linq;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavInt64<T> : TypedProperty<T, long> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavInt32{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    public DavInt64(XName name) 
        : base(name)
    {
        Converter = new Int64Converter();
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    protected override IConverter<long> Converter { get; }
}