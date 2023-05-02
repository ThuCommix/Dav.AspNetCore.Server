namespace Dav.AspNetCore.Server.Store.Properties;

public interface IConverter<T>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    object? Convert(T value);

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    T ConvertBack(object? value);
}