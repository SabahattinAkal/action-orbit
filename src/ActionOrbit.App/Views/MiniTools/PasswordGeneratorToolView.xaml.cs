using System.Windows;
using System.Windows.Input;
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

    private void PasswordBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        PasswordBox.Focus();
        PasswordBox.SelectAll();
        e.Handled = true;
    }

    private void SetLength_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string value }
            && int.TryParse(value, out var length))
        {
            LengthSlider.Value = length;
        }
    }

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
            var strength = GetStrengthAssessment(length, groupCount);
            StrengthText.Text = strength.Label;
            StrengthDescriptionText.Text = strength.Description;
            StrengthBar.Value = strength.Level;
            StatusText.Text = "Yeni parola güvenli rastgelelikle cihazında üretildi; hiçbir yere kaydedilmedi.";
        }
        catch (ArgumentException ex)
        {
            PasswordBox.Clear();
            StrengthText.Text = "Seçim gerekli";
            StrengthDescriptionText.Text = "En az bir grup seç";
            StrengthBar.Value = 0;
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

    private static PasswordStrengthAssessment GetStrengthAssessment(int length, int groupCount) =>
        (length, groupCount) switch
        {
            ( >= 24, >= 3) => new(4, "Çok güçlü", "Uzun ve yüksek çeşitlilik"),
            ( >= 16, >= 3) => new(3, "Güçlü", "İyi uzunluk ve çeşitlilik"),
            ( >= 14, >= 2) => new(2, "İyi", "Bir grup daha ekleyebilirsin"),
            _ => new(1, "Temel", "Uzunluğu veya çeşitliliği artır")
        };

    private sealed record PasswordStrengthAssessment(int Level, string Label, string Description);
}
