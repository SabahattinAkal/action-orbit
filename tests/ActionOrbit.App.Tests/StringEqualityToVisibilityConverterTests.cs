using System.Globalization;
using System.Windows;
using ActionOrbit.App.Converters;

namespace ActionOrbit.App.Tests;

public sealed class StringEqualityToVisibilityConverterTests
{
    private readonly StringEqualityToVisibilityConverter _converter = new();

    [Theory]
    [InlineData("default", "DEFAULT", Visibility.Visible)]
    [InlineData("browser", "default", Visibility.Collapsed)]
    [InlineData("", "default", Visibility.Collapsed)]
    public void Convert_UsesCaseInsensitiveStringEquality(
        string left,
        string right,
        Visibility expected)
    {
        var result = _converter.Convert(
            [left, right],
            typeof(Visibility),
            parameter: null!,
            CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }
}
