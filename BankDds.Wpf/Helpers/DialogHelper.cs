using System.Windows;

namespace BankDds.Wpf.Helpers;

public static class DialogHelper
{
    public static bool ShowConfirmation(string message, string title = "Confirmation")
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No
        );

        return result == MessageBoxResult.Yes;
    }

    public static void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }

    public static void ShowInformation(string message, string title = "Information")
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    public static void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning
        );
    }
}
