using System.Threading.Tasks;
using System.Windows;
using BankDds.Core.Interfaces;

namespace BankDds.Wpf.Services
{
    /// <summary>
    /// Handles DialogService responsibilities in the application.
    /// </summary>
    public class DialogService : IDialogService
    {
        public Task<bool> ShowConfirmationAsync(string message, string title = "Xác nhận")
        {
            var result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task ShowErrorAsync(string message, string title = "Lỗi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        public Task ShowInformationAsync(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public Task ShowWarningAsync(string message, string title = "Cảnh báo")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return Task.CompletedTask;
        }
    }
}
