using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class ReportService : IReportService
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ICustomerService _customerService;

    public ReportService(
        IAccountService accountService,
        ITransactionService transactionService,
        ICustomerService customerService)
    {
        _accountService = accountService;
        _transactionService = transactionService;
        _customerService = customerService;
    }

    public async Task<AccountStatement> GetAccountStatementAsync(string sotk, DateTime fromDate, DateTime toDate)
    {
        var account = await _accountService.GetAccountAsync(sotk);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        var allTransactions = await _transactionService.GetTransactionsByAccountAsync(sotk);

        // Calculate opening balance
        var openingTransactions = allTransactions.Where(t => t.NGAYGD < fromDate).ToList();
        decimal openingBalance = account.SODU;
        
        foreach (var trans in allTransactions.Where(t => t.NGAYGD >= fromDate))
        {
            if (trans.SOTK == sotk)
            {
                if (trans.LOAIGD == "GT")
                    openingBalance -= trans.SOTIEN;
                else if (trans.LOAIGD == "RT" || trans.LOAIGD == "CK")
                    openingBalance += trans.SOTIEN;
            }
            else if (trans.SOTK_NHAN == sotk)
            {
                openingBalance -= trans.SOTIEN;
            }
        }

        // Get transactions in period
        var periodTransactions = allTransactions
            .Where(t => t.NGAYGD >= fromDate && t.NGAYGD <= toDate)
            .OrderBy(t => t.NGAYGD)
            .ToList();

        // Calculate closing balance
        decimal closingBalance = openingBalance;
        foreach (var trans in periodTransactions)
        {
            if (trans.SOTK == sotk)
            {
                if (trans.LOAIGD == "GT")
                    closingBalance += trans.SOTIEN;
                else if (trans.LOAIGD == "RT" || trans.LOAIGD == "CK")
                    closingBalance -= trans.SOTIEN;
            }
            else if (trans.SOTK_NHAN == sotk)
            {
                closingBalance += trans.SOTIEN;
            }
        }

        return new AccountStatement
        {
            SOTK = sotk,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance,
            Transactions = periodTransactions,
            ClosingBalance = closingBalance
        };
    }

    public async Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate)
    {
        var allAccounts = await _accountService.GetAllAccountsAsync();
        return allAccounts
            .Where(a => a.NGAYMOTK >= fromDate && a.NGAYMOTK <= toDate)
            .OrderBy(a => a.NGAYMOTK)
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetCustomerCountByBranchAsync()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        return customers
            .GroupBy(c => c.MaCN)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<Customer>> GetCustomersByBranchReportAsync(string? branchCode = null)
    {
        var customers = await _customerService.GetAllCustomersAsync();
        
        if (!string.IsNullOrEmpty(branchCode))
        {
            customers = customers.Where(c => c.MaCN == branchCode).ToList();
        }
        
        return customers.OrderBy(c => c.FullName).ToList();
    }

    public async Task<TransactionSummary> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
    {
        List<Transaction> transactions;
        
        if (string.IsNullOrEmpty(branchCode))
        {
            // Get all transactions across all branches
            var allAccounts = await _accountService.GetAllAccountsAsync();
            var allTransactions = new List<Transaction>();
            
            foreach (var account in allAccounts)
            {
                var accountTransactions = await _transactionService.GetTransactionsByAccountAsync(account.SOTK);
                allTransactions.AddRange(accountTransactions);
            }
            
            transactions = allTransactions
                .Where(t => t.NGAYGD >= fromDate && t.NGAYGD <= toDate)
                .OrderByDescending(t => t.NGAYGD)
                .ToList();
        }
        else
        {
            // Get transactions for specific branch
            transactions = await _transactionService.GetTransactionsByBranchAsync(branchCode, fromDate, toDate);
        }

        // Remove duplicates (a transaction might be counted twice if it involves two accounts)
        var uniqueTransactions = transactions
            .GroupBy(t => t.MAGD)
            .Select(g => g.First())
            .ToList();

        var summary = new TransactionSummary
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchCode = branchCode,
            Transactions = uniqueTransactions,
            TotalTransactionCount = uniqueTransactions.Count,
            DepositCount = uniqueTransactions.Count(t => t.LOAIGD == "GT"),
            WithdrawalCount = uniqueTransactions.Count(t => t.LOAIGD == "RT"),
            TransferCount = uniqueTransactions.Count(t => t.LOAIGD == "CK"),
            TotalDepositAmount = uniqueTransactions.Where(t => t.LOAIGD == "GT").Sum(t => t.SOTIEN),
            TotalWithdrawalAmount = uniqueTransactions.Where(t => t.LOAIGD == "RT").Sum(t => t.SOTIEN),
            TotalTransferAmount = uniqueTransactions.Where(t => t.LOAIGD == "CK").Sum(t => t.SOTIEN)
        };

        return summary;
    }
}
