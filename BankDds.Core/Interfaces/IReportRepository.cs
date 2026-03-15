using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines report data queries for statement and summary outputs.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// Gets account statement in a date range.
        /// </summary>
        /// <param name="accountNumber">Account number.</param>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <returns>Account statement data for the requested account and date range.</returns>
        Task<AccountStatement?> GetAccountStatementAsync(string accountNumber, DateTime fromDate, DateTime toDate, string? customerCmnd = null);

        /// <summary>
        /// Gets accounts opened in a date range and optional branch.
        /// </summary>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A list of accounts opened in the requested period and optional branch scope.</returns>
        Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);

        /// <summary>
        /// Gets customer list for branch reporting.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A list of customers that belong to the requested branch.</returns>
        Task<List<Customer>> GetCustomersByBranchAsync(string? branchCode = null);

        /// <summary>
        /// Gets transaction summary and details in a date range.
        /// </summary>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>Transaction summary and detail data for the requested period and scope.</returns>
        Task<TransactionSummary?> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);

    }
}
