using System.Xml.Linq;
using Dav.AspNetCore.Server.Properties.Converters;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties.Primitives;

public class DavBool32<T> : TypedProperty<T, bool> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="DavBool32{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    public DavBool32(XName name)
        : base(name)
    {
        Converter = new Bool32Converter();
    }

    /// <summary>
    /// Gets the serializer.
    /// </summary>
    protected override IConverter<bool> Converter { get; }
}