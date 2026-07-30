namespace ActionOrbit.App.Services;

public interface IUserConfirmationService
{
    bool Confirm(string title, string message);
}

public sealed class MessageBoxConfirmationService : IUserConfirmationService
{
    public bool Confirm(string title, string message) =>
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes;
}
