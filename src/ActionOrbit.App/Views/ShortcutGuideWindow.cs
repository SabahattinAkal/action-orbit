using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ActionOrbit.App.Views;

internal sealed class ShortcutGuideWindow : Window
{
    internal ShortcutGuideWindow(string mainHotkey)
    {
        Title = "Action Orbit · Kısayollar";
        Width = 620; Height = 540; MinWidth = 460; MinHeight = 380;
        ShowInTaskbar = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "WindowBackgroundBrush");
        SetResourceReference(ForegroundProperty, "PrimaryTextBrush");
        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = "Daha az tıkla, daha hızlı ilerle", FontSize = 22, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = "Mouse makro tuşlarına global kombinasyonları atayabilirsin.", Margin = new Thickness(0, 7, 0, 18), TextWrapping = TextWrapping.Wrap });
        AddSection("HER YERDEN");
        AddRow(mainHotkey, "Aksiyon halkasını aç / kapat");
        AddRow("Ctrl+Alt+Shift+T", "Aktif Windows penceresini sabitle / bırak");
        AddSection("UYGULAMA VE EDİTÖR");
        AddRow("Ctrl+1…4", "Ana sayfa, editör, kütüphane ve ayarlar");
        AddRow("Ctrl+F", "Editörde aksiyon ara");
        AddRow("Ctrl+D", "Seçili aksiyonu alt aksiyonlarıyla çoğalt");
        AddRow("Alt+↑ / ↓", "Seçili aksiyonu aynı seviyede taşı");
        AddRow("Ctrl+Z", "Son düzenlemeyi geri al");
        AddRow("Ctrl+S", "Değişiklikleri kaydet");
        AddRow("F1", "Bu rehberi aç");
        AddSection("HALKA VE SHELF");
        AddRow("Oklar · Enter · Esc", "Halkada gezin, çalıştır, kapat");
        AddRow("Shelf küçük simgesi", "Tıkla: rafı aç · Sürükle: simgeyi taşı");
        Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        PreviewKeyDown += (_, args) => { if (args.Key == Key.Escape) { Close(); args.Handled = true; } };

        void AddSection(string text)
        {
            var heading = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 18, 0, 8) };
            heading.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
            content.Children.Add(heading);
        }
        void AddRow(string keys, string description)
        {
            var row = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(new TextBlock { Text = keys, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            var label = new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 0, 0, 0) };
            Grid.SetColumn(label, 1); row.Children.Add(label); content.Children.Add(row);
        }
    }
}
