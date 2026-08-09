using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ActionOrbit.App.Services.MiniTools;

namespace ActionOrbit.App.Views.MiniTools;

public partial class QuickNoteToolView : System.Windows.Controls.UserControl, IDisposable
{
    private readonly QuickNoteStore _store = new();
    private readonly DispatcherTimer _saveTimer;
    private bool _isLoading;
    private bool _hasPendingSave;

    public QuickNoteToolView()
    {
        InitializeComponent();
        _saveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(650)
        };
        _saveTimer.Tick += SaveTimer_Tick;
        LoadNote();
        Loaded += (_, _) => NoteBox.Focus();
    }

    public void Dispose()
    {
        _saveTimer.Stop();
        _saveTimer.Tick -= SaveTimer_Tick;
        if (_hasPendingSave)
        {
            SaveNote();
        }
    }

    private void LoadNote()
    {
        _isLoading = true;
        try
        {
            NoteBox.Text = _store.Load();
            SaveStateText.Text = "Kaydedildi";
        }
        catch (Exception ex)
        {
            NoteBox.Text = "";
            SaveStateText.Text = $"Not açılamadı: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            UpdateCount();
        }
    }

    private void NoteBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCount();
        if (_isLoading)
        {
            return;
        }

        _hasPendingSave = true;
        SaveStateText.Text = "Kaydediliyor…";
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveTimer_Tick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        SaveNote();
    }

    private void SaveNote()
    {
        try
        {
            _store.Save(NoteBox.Text);
            _hasPendingSave = false;
            SaveStateText.Text = $"Kaydedildi · {DateTime.Now:HH:mm}";
        }
        catch (Exception ex)
        {
            SaveStateText.Text = $"Kaydedilemedi: {ex.Message}";
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(NoteBox.Text ?? "");
            SaveStateText.Text = "Not panoya kopyalandı";
        }
        catch (Exception)
        {
            SaveStateText.Text = "Pano şu anda kullanılıyor";
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        NoteBox.Clear();
        NoteBox.Focus();
    }

    private void UpdateCount() => CountText.Text = $"{NoteBox.Text.Length:N0} karakter";
}
