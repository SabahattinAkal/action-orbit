using System.Windows;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class PasswordGeneratorToolView : System.Windows.Controls.UserControl
{
    public PasswordGeneratorToolView()
    {
        InitializeComponent();
        Loaded += (_, _) => GeneratePassword();
    }

    private void LengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LengthText is null)
        {
            return;
        }

        LengthText.Text = $"{(int)e.NewValue} karakter";
        if (IsLoaded)
        {
            GeneratePassword();
        }
    }

    private void Option_Click(object sender, RoutedEventArgs e) => GeneratePassword();

    private void Generate_Click(object sender, RoutedEventArgs e) => GeneratePassword();

    private void GeneratePassword()
    {
        try
        {
            var length = (int)LengthSlider.Value;
            var groupCount = new[]
            {
                LowercaseBox.IsChecked == true,
                UppercaseBox.IsChecked == true,
                DigitsBox.IsChecked == true,
                SymbolsBox.IsChecked == true
            }.Count(selected => selected);

            PasswordBox.Text = PasswordGenerator.Generate(
                length,
                LowercaseBox.IsChecked == true,
                UppercaseBox.IsChecked == true,
                DigitsBox.IsChecked == true,
                SymbolsBox.IsChecked == true);
            StrengthText.Text = GetStrengthLabel(length, groupCount);
            StatusText.Text = "Yeni parola üretildi; hiçbir yere kaydedilmedi.";
        }
        catch (ArgumentException ex)
        {
            PasswordBox.Clear();
            StrengthText.Text = "Seçim gerekli";
            StatusText.Text = ex.Message;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordBox.Text))
        {
            StatusText.Text = "Önce en az bir karakter grubu seç.";
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(PasswordBox.Text);
            StatusText.Text = "Parola panoya kopyalandı.";
        }
        catch (Exception)
        {
            StatusText.Text = "Pano şu anda kullanılıyor.";
        }
    }

    private static string GetStrengthLabel(int length, int groupCount) => (length, groupCount) switch
    {
        ( >= 24, >= 3) => "Çok güçlü",
        ( >= 16, >= 3) => "Güçlü",
        ( >= 14, >= 2) => "İyi",
        _ => "Temel"
    };
}
