using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class AccountService : IAccountService
{
    private readonly List<Account> _accounts = new()
    {
        new Account { SOTK = "001001", CMND = "123456", SODU = 10000000, MACN = "BENTHANH", NGAYMOTK = DateTime.Now.AddMonths(-6) },
        new Account { SOTK = "001002", CMND = "234567", SODU = 5000000, MACN = "BENTHANH", NGAYMOTK = DateTime.Now.AddMonths(-3) },
        new Account { SOTK = "002001", CMND = "345678", SODU = 8000000, MACN = "TANDINH", NGAYMOTK = DateTime.Now.AddMonths(-12) },
        new Account { SOTK = "002002", CMND = "456789", SODU = 15000000, MACN = "TANDINH", NGAYMOTK = DateTime.Now.AddMonths(-8) },
        new Account { SOTK = "001003", CMND = "c123456", SODU = 3000000, MACN = "BENTHANH", NGAYMOTK = DateTime.Now.AddMonths(-1) }
    };

    public Task<List<Account>> GetAccountsByBranchAsync(string branchCode)
    {
        var accounts = _accounts.Where(a => a.MACN == branchCode).ToList();
        return Task.FromResult(accounts);
    }

    public Task<List<Account>> GetAllAccountsAsync()
    {
        return Task.FromResult(_accounts.ToList());
    }

    public Task<List<Account>> GetAccountsByCustomerAsync(string cmnd)
    {
        var accounts = _accounts.Where(a => a.CMND == cmnd).ToList();
        return Task.FromResult(accounts);
    }

    public Task<Account?> GetAccountAsync(string sotk)
    {
        var account = _accounts.FirstOrDefault(a => a.SOTK == sotk);
        return Task.FromResult(account);
    }

    public Task<bool> AddAccountAsync(Account account)
    {
        if (_accounts.Any(a => a.SOTK == account.SOTK))
            return Task.FromResult(false);

        _accounts.Add(account);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateAccountAsync(Account account)
    {
        var existing = _accounts.FirstOrDefault(a => a.SOTK == account.SOTK);
        if (existing == null)
            return Task.FromResult(false);

        existing.SODU = account.SODU;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAccountAsync(string sotk)
    {
        var account = _accounts.FirstOrDefault(a => a.SOTK == sotk);
        if (account == null)
            return Task.FromResult(false);

        if (account.SODU != 0)
            return Task.FromResult(false); // Cannot delete account with balance

        _accounts.Remove(account);
        return Task.FromResult(true);
    }
}
