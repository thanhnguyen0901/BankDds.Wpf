using BankDds.Core.Models;

namespace BankDds.Wpf.Services
{
    public interface IReportExportService
    {
        Task ExportStatementToPdfAsync(AccountStatement statement, string filePath);
        Task ExportStatementToExcelAsync(AccountStatement statement, string filePath);
        Task ExportAccountsToPdfAsync(List<Account> accounts, DateTime fromDate, DateTime toDate, string filePath);
        Task ExportAccountsToExcelAsync(List<Account> accounts, DateTime fromDate, DateTime toDate, string filePath);
        Task ExportCustomersToPdfAsync(List<Customer> customers, string? branchCode, string filePath);
        Task ExportCustomersToExcelAsync(List<Customer> customers, string? branchCode, string filePath);
        Task ExportTransactionSummaryToPdfAsync(TransactionSummary summary, string filePath);
        Task ExportTransactionSummaryToExcelAsync(TransactionSummary summary, string filePath);
    }
}