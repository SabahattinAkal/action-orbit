using System.Globalization;

namespace ActionOrbit.App.Services.MiniTools;

internal sealed record UnitDefinition(string Key, string Label, string Symbol);

internal sealed record UnitCategory(
    string Key,
    string Label,
    IReadOnlyList<UnitDefinition> Units);

internal static class UnitConversionEngine
{
    public static IReadOnlyList<UnitCategory> Categories { get; } =
    [
        new("length", "Uzunluk",
        [
            new("meter", "Metre", "m"),
            new("kilometer", "Kilometre", "km"),
            new("centimeter", "Santimetre", "cm"),
            new("millimeter", "Milimetre", "mm"),
            new("inch", "İnç", "in"),
            new("foot", "Fit", "ft")
        ]),
        new("weight", "Ağırlık",
        [
            new("kilogram", "Kilogram", "kg"),
            new("gram", "Gram", "g"),
            new("pound", "Pound", "lb"),
            new("ounce", "Ons", "oz")
        ]),
        new("temperature", "Sıcaklık",
        [
            new("celsius", "Santigrat", "°C"),
            new("fahrenheit", "Fahrenhayt", "°F"),
            new("kelvin", "Kelvin", "K")
        ]),
        new("data", "Veri",
        [
            new("byte", "Bayt", "B"),
            new("kilobyte", "Kilobayt", "KB"),
            new("megabyte", "Megabayt", "MB"),
            new("gigabyte", "Gigabayt", "GB")
        ])
    ];

    public static bool TryConvert(
        string? categoryKey,
        string? fromKey,
        string? toKey,
        double value,
        out double result)
    {
        result = 0;
        if (!double.IsFinite(value))
        {
            return false;
        }

        try
        {
            var baseValue = categoryKey switch
            {
                "length" => ToLengthMeters(fromKey, value),
                "weight" => ToWeightKilograms(fromKey, value),
                "temperature" => ToCelsius(fromKey, value),
                "data" => ToBytes(fromKey, value),
                _ => null
            };

            if (baseValue is null)
            {
                return false;
            }

            result = categoryKey switch
            {
                "length" => FromLengthMeters(toKey, baseValue.Value),
                "weight" => FromWeightKilograms(toKey, baseValue.Value),
                "temperature" => FromCelsius(toKey, baseValue.Value),
                "data" => FromBytes(toKey, baseValue.Value),
                _ => null
            } ?? double.NaN;

            return double.IsFinite(result);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static bool TryParseValue(string? text, out double value)
    {
        const NumberStyles styles = NumberStyles.Float;
        var input = text?.Trim() ?? "";
        return double.TryParse(input, styles, CultureInfo.CurrentCulture, out value)
            || double.TryParse(input, styles, CultureInfo.GetCultureInfo("tr-TR"), out value)
            || double.TryParse(input, styles, CultureInfo.InvariantCulture, out value);
    }

    private static double? ToLengthMeters(string? key, double value) => key switch
    {
        "meter" => value,
        "kilometer" => value * 1000,
        "centimeter" => value / 100,
        "millimeter" => value / 1000,
        "inch" => value * 0.0254,
        "foot" => value * 0.3048,
        _ => null
    };

    private static double? FromLengthMeters(string? key, double value) => key switch
    {
        "meter" => value,
        "kilometer" => value / 1000,
        "centimeter" => value * 100,
        "millimeter" => value * 1000,
        "inch" => value / 0.0254,
        "foot" => value / 0.3048,
        _ => null
    };

    private static double? ToWeightKilograms(string? key, double value) => key switch
    {
        "kilogram" => value,
        "gram" => value / 1000,
        "pound" => value * 0.45359237,
        "ounce" => value * 0.028349523125,
        _ => null
    };

    private static double? FromWeightKilograms(string? key, double value) => key switch
    {
        "kilogram" => value,
        "gram" => value * 1000,
        "pound" => value / 0.45359237,
        "ounce" => value / 0.028349523125,
        _ => null
    };

    private static double? ToCelsius(string? key, double value) => key switch
    {
        "celsius" => value,
        "fahrenheit" => (value - 32) * 5 / 9,
        "kelvin" => value - 273.15,
        _ => null
    };

    private static double? FromCelsius(string? key, double value) => key switch
    {
        "celsius" => value,
        "fahrenheit" => value * 9 / 5 + 32,
        "kelvin" => value + 273.15,
        _ => null
    };

    private static double? ToBytes(string? key, double value) => key switch
    {
        "byte" => value,
        "kilobyte" => value * 1024,
        "megabyte" => value * 1024 * 1024,
        "gigabyte" => value * 1024 * 1024 * 1024,
        _ => null
    };

    private static double? FromBytes(string? key, double value) => key switch
    {
        "byte" => value,
        "kilobyte" => value / 1024,
        "megabyte" => value / (1024 * 1024),
        "gigabyte" => value / (1024 * 1024 * 1024),
        _ => null
    };
}
