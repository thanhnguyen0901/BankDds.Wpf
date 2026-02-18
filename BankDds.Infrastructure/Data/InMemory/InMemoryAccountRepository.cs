using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory implementation of IAccountRepository for development and testing.
/// Uses locking to ensure thread-safe atomic operations.
/// </summary>
public class InMemoryAccountRepository : IAccountRepository
{
    private readonly List<Account> _accounts = new()
    {
        // CMND values match InMemoryCustomerRepository seed; SOTK = nChar(9).
        new Account { SOTK = "TK0000001", CMND = "012345678", SODU = 10000000, MACN = "BENTHANH", NGAYMOTK = DateTime.Now.AddMonths(-6),  Status = "Active" },
        new Account { SOTK = "TK0000002", CMND = "023456789", SODU =  5000000, MACN = "BENTHANH", NGAYMOTK = DateTime.Now.AddMonths(-3),  Status = "Active" },
        new Account { SOTK = "TK0000003", CMND = "034567890", SODU =  8000000, MACN = "TANDINH",  NGAYMOTK = DateTime.Now.AddMonths(-12), Status = "Active" },
        new Account { SOTK = "TK0000004", CMND = "045678901", SODU = 15000000, MACN = "TANDINH",  NGAYMOTK = DateTime.Now.AddMonths(-8),  Status = "Active" },
        new Account { SOTK = "TK0000005", CMND = "056789012", SODU =  3000000, MACN = "BENTHANH", NGAYMOTK = DateTime.Now.AddMonths(-1),  Status = "Active" }
    };

    private readonly object _lock = new object();

    public Task<List<Account>> GetAccountsByBranchAsync(string branchCode)
    {
        lock (_lock)
        {
            var accounts = _accounts.Where(a => a.MACN == branchCode).ToList();
            return Task.FromResult(accounts);
        }
    }

    public Task<List<Account>> GetAllAccountsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_accounts.ToList());
        }
    }

    public Task<List<Account>> GetAccountsByCustomerAsync(string cmnd)
    {
        lock (_lock)
        {
            var accounts = _accounts.Where(a => a.CMND == cmnd).ToList();
            return Task.FromResult(accounts);
        }
    }

    public Task<Account?> GetAccountAsync(string sotk)
    {
        lock (_lock)
        {
            var account = _accounts.FirstOrDefault(a => a.SOTK == sotk);
            return Task.FromResult(account);
        }
    }

    public Task<bool> AddAccountAsync(Account account)
    {
        lock (_lock)
        {
            if (_accounts.Any(a => a.SOTK == account.SOTK))
                return Task.FromResult(false);

            _accounts.Add(account);
            return Task.FromResult(true);
        }
    }

    public Task<bool> UpdateAccountAsync(Account account)
    {
        lock (_lock)
        {
            var existing = _accounts.FirstOrDefault(a => a.SOTK == account.SOTK);
            if (existing == null)
                return Task.FromResult(false);

            existing.SODU = account.SODU;
            existing.Status = account.Status;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAccountAsync(string sotk)
    {
        lock (_lock)
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

    public Task<bool> CloseAccountAsync(string sotk)
    {
        lock (_lock)
        {
            var account = _accounts.FirstOrDefault(a => a.SOTK == sotk);
            if (account == null)
                return Task.FromResult(false);

            if (account.SODU != 0)
                return Task.FromResult(false); // Cannot close account with non-zero balance

            account.Status = "Closed";
            return Task.FromResult(true);
        }
    }

    public Task<bool> ReopenAccountAsync(string sotk)
    {
        lock (_lock)
        {
            var account = _accounts.FirstOrDefault(a => a.SOTK == sotk);
            if (account == null)
                return Task.FromResult(false);

            if (account.Status != "Closed")
                return Task.FromResult(false); // Account must be closed to reopen

            account.Status = "Active";
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Atomically transfers money between two accounts.
    /// All validation (same account, status, balance) is re-checked inside the
    /// lock so the result is correct even under concurrent withdrawals or
    /// account-close operations that race with the caller's pre-checks.
    /// </summary>
    public Task<bool> AtomicTransferAsync(string sotkFrom, string sotkTo, decimal amount)
    {
        lock (_lock)
        {
            // Guard: same account
            if (sotkFrom == sotkTo)
                return Task.FromResult(false);

            var accountFrom = _accounts.FirstOrDefault(a => a.SOTK == sotkFrom);
            var accountTo   = _accounts.FirstOrDefault(a => a.SOTK == sotkTo);

            if (accountFrom == null || accountTo == null)
                return Task.FromResult(false);

            // Guard: both accounts must be active (re-checked inside lock)
            if (accountFrom.Status == "Closed" || accountTo.Status == "Closed")
                return Task.FromResult(false);

            // Guard: sufficient balance (re-checked inside lock)
            if (accountFrom.SODU < amount)
                return Task.FromResult(false);

            // Atomic: both mutations happen in the same lock acquisition
            accountFrom.SODU -= amount;
            accountTo.SODU   += amount;

            return Task.FromResult(true);
        }
    }
}
