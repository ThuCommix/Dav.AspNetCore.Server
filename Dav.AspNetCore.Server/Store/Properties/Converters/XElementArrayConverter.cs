using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties.Converters;

public class XElementArrayConverter : IConverter<IEnumerable<XElement>>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object? Convert(IEnumerable<XElement>? value) => value;

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public IEnumerable<XElement> ConvertBack(object? value) => value == null ? Array.Empty<XElement>() : (IEnumerable<XElement>)value;
}