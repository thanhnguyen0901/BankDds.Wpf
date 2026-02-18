using BankDds.Core.Models;

namespace BankDds.Wpf.Services;

public interface IReportExportService
{
    /// <summary>
    /// Export account statement to PDF
    /// </summary>
    Task ExportStatementToPdfAsync(AccountStatement statement, string filePath);

    /// <summary>
    /// Export account statement to Excel
    /// </summary>
    Task ExportStatementToExcelAsync(AccountStatement statement, string filePath);

    /// <summary>
    /// Export list of accounts to PDF
    /// </summary>
    Task ExportAccountsToPdfAsync(List<Account> accounts, DateTime fromDate, DateTime toDate, string filePath);

    /// <summary>
    /// Export list of accounts to Excel
    /// </summary>
    Task ExportAccountsToExcelAsync(List<Account> accounts, DateTime fromDate, DateTime toDate, string filePath);

    /// <summary>
    /// Export list of customers to PDF
    /// </summary>
    Task ExportCustomersToPdfAsync(List<Customer> customers, string? branchCode, string filePath);

    /// <summary>
    /// Export list of customers to Excel
    /// </summary>
    Task ExportCustomersToExcelAsync(List<Customer> customers, string? branchCode, string filePath);

    /// <summary>
    /// Export transaction summary to PDF
    /// </summary>
    Task ExportTransactionSummaryToPdfAsync(TransactionSummary summary, string filePath);

    /// <summary>
    /// Export transaction summary to Excel
    /// </summary>
    Task ExportTransactionSummaryToExcelAsync(TransactionSummary summary, string filePath);
}
