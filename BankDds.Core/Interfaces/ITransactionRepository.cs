using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines persistence operations for deposit, withdrawal, and transfer transactions.
    /// </summary>
    public interface ITransactionRepository
    {
        /// <summary>
        /// Gets transactions of an account.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <returns>A list of transactions of the requested account.</returns>
        Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk);

        /// <summary>
        /// Gets transactions of a branch in optional date range.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <returns>A list of transactions in the requested branch and optional date range.</returns>
        Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Performs a deposit transaction.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <param name="amount">Transaction amount.</param>
        /// <param name="manv">Employee code.</param>
        /// <returns>True when the deposit transaction is posted successfully; otherwise false.</returns>
        Task<bool> DepositAsync(string sotk, decimal amount, string manv);

        /// <summary>
        /// Performs a withdrawal transaction.
        /// </summary>
        /// <param name="sotk">Account number.</param>
        /// <param name="amount">Transaction amount.</param>
        /// <param name="manv">Employee code.</param>
        /// <returns>True when the withdrawal transaction is posted successfully; otherwise false.</returns>
        Task<bool> WithdrawAsync(string sotk, decimal amount, string manv);

        /// <summary>
        /// Performs a transfer transaction between accounts.
        /// </summary>
        /// <param name="sotkFrom">Source account number.</param>
        /// <param name="sotkTo">Destination account number.</param>
        /// <param name="amount">Transaction amount.</param>
        /// <param name="manv">Employee code.</param>
        /// <returns>True when the transfer transaction is posted successfully; otherwise false.</returns>
        Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv);

        /// <summary>
        /// Gets total withdrawn amount of an account in a day.
        /// </summary>
        /// <param name="accountNumber">Account number.</param>
        /// <param name="date">Business date.</param>
        /// <returns>Total withdrawal amount of the account for the business date.</returns>
        Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date);

        /// <summary>
        /// Gets total transferred amount of an account in a day.
        /// </summary>
        /// <param name="accountNumber">Account number.</param>
        /// <param name="date">Business date.</param>
        /// <returns>Total transfer-out amount of the account for the business date.</returns>
        Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date);

    }
}
