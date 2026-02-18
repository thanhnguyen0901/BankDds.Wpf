using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository interface for Report data access operations
/// </summary>
public interface IReportRepository
{
    Task<AccountStatement?> GetAccountStatementAsync(string accountNumber, DateTime fromDate, DateTime toDate);
    Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);
    Task<List<Customer>> GetCustomersByBranchAsync(string? branchCode = null);
    Task<TransactionSummary?> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null);
}
