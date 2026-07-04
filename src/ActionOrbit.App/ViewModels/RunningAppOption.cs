namespace ActionOrbit.App.ViewModels;

public sealed record RunningAppOption(string ProcessName, string WindowTitle)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(WindowTitle)
            ? ProcessName
            : $"{WindowTitle}  ({ProcessName})";
}
