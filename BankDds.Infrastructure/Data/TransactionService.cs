using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class TransactionService : ITransactionService
{
    private readonly List<Transaction> _transactions = new()
    {
        new Transaction { MAGD = "GD001", SOTK = "001001", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-10), SOTIEN = 2000000, MANV = 1 },
        new Transaction { MAGD = "GD002", SOTK = "001001", LOAIGD = "RT", NGAYGD = DateTime.Now.AddDays(-5), SOTIEN = 500000, MANV = 1 },
        new Transaction { MAGD = "GD003", SOTK = "001002", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-7), SOTIEN = 1000000, MANV = 2 },
        new Transaction { MAGD = "GD004", SOTK = "002001", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-3), SOTIEN = 3000000, MANV = 3 },
        new Transaction { MAGD = "GD005", SOTK = "001003", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-1), SOTIEN = 500000, MANV = 1 }
    };

    private readonly IAccountService _accountService;
    private int _nextId = 6;

    public TransactionService(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk)
    {
        var transactions = _transactions
            .Where(t => t.SOTK == sotk || t.SOTK_NHAN == sotk)
            .OrderByDescending(t => t.NGAYGD)
            .ToList();
        return Task.FromResult(transactions);
    }

    public async Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var accounts = await _accountService.GetAccountsByBranchAsync(branchCode);
        var accountNumbers = accounts.Select(a => a.SOTK).ToList();

        var transactions = _transactions
            .Where(t => accountNumbers.Contains(t.SOTK))
            .Where(t => !fromDate.HasValue || t.NGAYGD >= fromDate.Value)
            .Where(t => !toDate.HasValue || t.NGAYGD <= toDate.Value)
            .OrderByDescending(t => t.NGAYGD)
            .ToList();

        return transactions;
    }

    public async Task<bool> DepositAsync(string sotk, decimal amount, int manv)
    {
        if (amount <= 0)
            return false;

        var account = await _accountService.GetAccountAsync(sotk);
        if (account == null)
            return false;

        account.SODU += amount;
        await _accountService.UpdateAccountAsync(account);

        var transaction = new Transaction
        {
            MAGD = $"GD{_nextId++:D3}",
            SOTK = sotk,
            LOAIGD = "GT",
            NGAYGD = DateTime.Now,
            SOTIEN = amount,
            MANV = manv
        };

        _transactions.Add(transaction);
        return true;
    }

    public async Task<bool> WithdrawAsync(string sotk, decimal amount, int manv)
    {
        if (amount <= 0)
            return false;

        var account = await _accountService.GetAccountAsync(sotk);
        if (account == null)
            return false;

        if (account.SODU < amount)
            return false;

        account.SODU -= amount;
        await _accountService.UpdateAccountAsync(account);

        var transaction = new Transaction
        {
            MAGD = $"GD{_nextId++:D3}",
            SOTK = sotk,
            LOAIGD = "RT",
            NGAYGD = DateTime.Now,
            SOTIEN = amount,
            MANV = manv
        };

        _transactions.Add(transaction);
        return true;
    }

    public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, int manv)
    {
        if (amount <= 0)
            return false;

        var accountFrom = await _accountService.GetAccountAsync(sotkFrom);
        var accountTo = await _accountService.GetAccountAsync(sotkTo);

        if (accountFrom == null || accountTo == null)
            return false;

        if (accountFrom.SODU < amount)
            return false;

        accountFrom.SODU -= amount;
        accountTo.SODU += amount;

        await _accountService.UpdateAccountAsync(accountFrom);
        await _accountService.UpdateAccountAsync(accountTo);

        var transaction = new Transaction
        {
            MAGD = $"GD{_nextId++:D3}",
            SOTK = sotkFrom,
            LOAIGD = "CK",
            NGAYGD = DateTime.Now,
            SOTIEN = amount,
            MANV = manv,
            SOTK_NHAN = sotkTo
        };

        _transactions.Add(transaction);
        return true;
    }
}
