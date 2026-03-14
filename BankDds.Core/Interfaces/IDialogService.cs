namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines UI dialog interactions for confirmation and notifications.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows confirmation dialog and returns user choice.
        /// </summary>
        /// <param name="message">Dialog message text.</param>
        /// <param name="title">Dialog title text.</param>
        /// <returns>True when user confirms the action; otherwise false.</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation");

        /// <summary>
        /// Shows error dialog.
        /// </summary>
        /// <param name="message">Dialog message text.</param>
        /// <param name="title">Dialog title text.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task ShowErrorAsync(string message, string title = "Error");

        /// <summary>
        /// Shows information dialog.
        /// </summary>
        /// <param name="message">Dialog message text.</param>
        /// <param name="title">Dialog title text.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task ShowInformationAsync(string message, string title = "Information");

        /// <summary>
        /// Shows warning dialog.
        /// </summary>
        /// <param name="message">Dialog message text.</param>
        /// <param name="title">Dialog title text.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task ShowWarningAsync(string message, string title = "Warning");

    }
}
