using System.Windows;
using System.Windows.Controls;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class TextToolsToolView : System.Windows.Controls.UserControl
{
    public TextToolsToolView()
    {
        InitializeComponent();
        Loaded += (_, _) => TextBox.Focus();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = TextBox.Text ?? "";
        var lineCount = text.Length == 0 ? 0 : text.Count(character => character == '\n') + 1;
        CountText.Text = $"{TextTransformService.CountWords(text):N0} kelime · {text.Length:N0} karakter · {lineCount:N0} satır";
    }

    private void Transform_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string operation })
        {
            return;
        }

        var selectionStart = TextBox.SelectionStart;
        TextBox.Text = operation switch
        {
            "upper" => TextTransformService.ToUpper(TextBox.Text),
            "lower" => TextTransformService.ToLower(TextBox.Text),
            "title" => TextTransformService.ToTitleCase(TextBox.Text),
            "spaces" => TextTransformService.NormalizeWhitespace(TextBox.Text),
            _ => TextBox.Text
        };
        TextBox.SelectionStart = Math.Min(selectionStart, TextBox.Text.Length);
        TextBox.Focus();
        StatusText.Text = "Dönüşüm uygulandı.";
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(TextBox.Text ?? "");
            StatusText.Text = "Metin panoya kopyalandı.";
        }
        catch (Exception)
        {
            StatusText.Text = "Pano şu anda kullanılıyor.";
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        TextBox.Clear();
        TextBox.Focus();
        StatusText.Text = "Metin temizlendi.";
    }
}
