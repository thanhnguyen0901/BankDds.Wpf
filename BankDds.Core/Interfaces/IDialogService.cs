namespace BankDds.Core.Interfaces;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation");
    Task ShowErrorAsync(string message, string title = "Error");
    Task ShowInformationAsync(string message, string title = "Information");
    Task ShowWarningAsync(string message, string title = "Warning");
}
