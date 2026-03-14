using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines report use cases consumed by presentation layer.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Gets account statement in a date range.
        /// </summary>
        /// <param name="sotk">Account number used to build statement data.</param>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <returns>Account statement data for the requested account and date range.</returns>
        Task<AccountStatement> GetAccountStatementAsync(string sotk, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Gets accounts opened in a date range and optional branch.
        /// </summary>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A list of accounts opened in the requested period and optional branch scope.</returns>
        Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);

        /// <summary>
        /// Gets customer count grouped by branch.
        /// </summary>
        /// <returns>Customer totals grouped by branch code.</returns>
        Task<Dictionary<string, int>> GetCustomerCountByBranchAsync();

        /// <summary>
        /// Gets customer list for branch report output.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A customer list formatted for branch reporting scope.</returns>
        Task<List<Customer>> GetCustomersByBranchReportAsync(string? branchCode = null);

        /// <summary>
        /// Gets transaction summary and details in a date range.
        /// </summary>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date.</param>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>Transaction summary and detail data for the requested period and scope.</returns>
        Task<TransactionSummary> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);

    }
}
