using System.Text;

namespace ActionOrbit.App.Services.MiniTools;

internal sealed class QuickNoteStore
{
    internal const int MaxNoteBytes = 128 * 1024;
    private readonly string _notePath;

    public QuickNoteStore(string? appDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(appDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActionOrbitPro")
            : appDirectory;
        _notePath = Path.Combine(directory, "quick-note.txt");
    }

    public string NotePath => _notePath;

    public string Load()
    {
        if (!File.Exists(_notePath))
        {
            return "";
        }

        var info = new FileInfo(_notePath);
        if (info.Length > MaxNoteBytes)
        {
            throw new InvalidDataException("Hızlı not dosyası güvenli boyut sınırını aşıyor.");
        }

        return File.ReadAllText(_notePath, Encoding.UTF8);
    }

    public void Save(string? text)
    {
        var value = text ?? "";
        if (Encoding.UTF8.GetByteCount(value) > MaxNoteBytes)
        {
            throw new InvalidOperationException($"Hızlı not en fazla {MaxNoteBytes / 1024} KB olabilir.");
        }

        var directory = Path.GetDirectoryName(_notePath)
            ?? throw new InvalidOperationException("Hızlı not dizini bulunamadı.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_notePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, value, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, _notePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
