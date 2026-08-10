using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ActionOrbit.App.Converters;

public sealed class StringEqualityToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2
            || values[0] is not string left
            || values[1] is not string right
            || string.IsNullOrWhiteSpace(left)
            || string.IsNullOrWhiteSpace(right))
        {
            return Visibility.Collapsed;
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
