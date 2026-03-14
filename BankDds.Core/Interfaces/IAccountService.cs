using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines account business operations used by application flows.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Gets accounts that belong to a specific branch.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A list of accounts opened at the specified branch.</returns>
        Task<List<Account>> GetAccountsByBranchAsync(string branchCode);

        /// <summary>
        /// Gets all accounts visible to current scope.
        /// </summary>
        /// <returns>A list of accounts available in the current data scope.</returns>
        Task<List<Account>> GetAllAccountsAsync();

        /// <summary>
        /// Gets all accounts owned by a customer.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>A list of accounts owned by the specified customer.</returns>
        Task<List<Account>> GetAccountsByCustomerAsync(string cmnd);

        /// <summary>
        /// Gets account details by account number.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <returns>The account details when found; otherwise null.</returns>
        Task<Account?> GetAccountAsync(string sotk);

        /// <summary>
        /// Creates a new account for a customer.
        /// </summary>
        /// <param name="account">Account entity.</param>
        /// <returns>True when a new account is created successfully; otherwise false.</returns>
        Task<bool> AddAccountAsync(Account account);

        /// <summary>
        /// Updates editable account information.
        /// </summary>
        /// <param name="account">Account entity.</param>
        /// <returns>True when account information is updated successfully; otherwise false.</returns>
        Task<bool> UpdateAccountAsync(Account account);

        /// <summary>
        /// Deletes an account when business conditions are satisfied.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <returns>True when the account is deleted successfully; otherwise false.</returns>
        Task<bool> DeleteAccountAsync(string sotk);

        /// <summary>
        /// Marks an account as closed.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <returns>True when the account status changes to closed; otherwise false.</returns>
        Task<bool> CloseAccountAsync(string sotk);

        /// <summary>
        /// Reopens a previously closed account.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <returns>True when the account status changes back to active; otherwise false.</returns>
        Task<bool> ReopenAccountAsync(string sotk);

    }
}
