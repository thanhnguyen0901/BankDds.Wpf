using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IReportService
{
    Task<AccountStatement> GetAccountStatementAsync(string sotk, DateTime fromDate, DateTime toDate);
    Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);
    Task<Dictionary<string, int>> GetCustomerCountByBranchAsync();
    Task<List<Customer>> GetCustomersByBranchReportAsync(string? branchCode = null);
    Task<TransactionSummary> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);
}
