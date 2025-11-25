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
}
