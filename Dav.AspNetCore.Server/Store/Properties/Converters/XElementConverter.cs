using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties.Converters;

public class XElementConverter : IConverter<XElement?>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object? Convert(XElement? value) => value;

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public XElement? ConvertBack(object? value) => (XElement?)value;
}