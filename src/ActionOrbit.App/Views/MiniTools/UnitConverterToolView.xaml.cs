using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class UnitConverterToolView : System.Windows.Controls.UserControl
{
    private string? _resultValue;
    private bool _isUpdating;

    public UnitConverterToolView()
    {
        InitializeComponent();
        CategoryBox.ItemsSource = UnitConversionEngine.Categories;
        CategoryBox.SelectedIndex = 0;
        Loaded += (_, _) => ValueBox.Focus();
    }

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryBox.SelectedItem is not UnitCategory category)
        {
            return;
        }

        _isUpdating = true;
        FromUnitBox.ItemsSource = category.Units;
        ToUnitBox.ItemsSource = category.Units;
        FromUnitBox.SelectedIndex = 0;
        ToUnitBox.SelectedIndex = Math.Min(1, category.Units.Count - 1);
        _isUpdating = false;
        ConvertValue();
    }

    private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            ConvertValue();
        }
    }

    private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            ConvertValue();
        }
    }

    private void Swap_Click(object sender, RoutedEventArgs e)
    {
        if (FromUnitBox.SelectedItem is not UnitDefinition from
            || ToUnitBox.SelectedItem is not UnitDefinition to)
        {
            return;
        }

        _isUpdating = true;
        FromUnitBox.SelectedItem = to;
        ToUnitBox.SelectedItem = from;
        if (!string.IsNullOrWhiteSpace(_resultValue))
        {
            ValueBox.Text = _resultValue;
        }

        _isUpdating = false;
        ConvertValue();
    }

    private void ConvertValue()
    {
        if (CategoryBox.SelectedItem is not UnitCategory category
            || FromUnitBox.SelectedItem is not UnitDefinition from
            || ToUnitBox.SelectedItem is not UnitDefinition to)
        {
            return;
        }

        if (!UnitConversionEngine.TryParseValue(ValueBox.Text, out var value))
        {
            _resultValue = null;
            ResultText.Text = "—";
            StatusText.Text = "Geçerli bir sayı gir.";
            return;
        }

        if (!UnitConversionEngine.TryConvert(category.Key, from.Key, to.Key, value, out var result))
        {
            _resultValue = null;
            ResultText.Text = "—";
            StatusText.Text = "Bu dönüşüm hesaplanamadı.";
            return;
        }

        _resultValue = result.ToString("0.##########", CultureInfo.CurrentCulture);
        ResultText.Text = $"{_resultValue} {to.Symbol}";
        StatusText.Text = $"{from.Symbol} → {to.Symbol} · hesaplama tamamen yerel";
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_resultValue))
        {
            StatusText.Text = "Önce geçerli bir dönüşüm yap.";
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(_resultValue);
            StatusText.Text = "Sonuç panoya kopyalandı.";
        }
        catch (Exception)
        {
            StatusText.Text = "Pano şu anda kullanılıyor.";
        }
    }

}
