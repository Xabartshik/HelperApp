using System.Collections;
using System.Globalization;

namespace HelperApp.Converters;

public class EmptyCollectionToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
        {
            return collection.Count == 0;
        }

        if (value is IEnumerable enumerable)
        {
            return !enumerable.Cast<object>().Any();
        }

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
