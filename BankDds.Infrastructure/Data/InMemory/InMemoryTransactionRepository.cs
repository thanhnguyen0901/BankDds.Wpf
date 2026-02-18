using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory implementation of ITransactionRepository for development and testing
/// </summary>
public class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _transactions = new()
    {
        new Transaction { MAGD = "GD001", SOTK = "TK0000001", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-10), SOTIEN = 2000000, MANV = "NV00000001", Status = "Completed" },
        new Transaction { MAGD = "GD002", SOTK = "TK0000001", LOAIGD = "RT", NGAYGD = DateTime.Now.AddDays(-5), SOTIEN = 500000, MANV = "NV00000001", Status = "Completed" },
        new Transaction { MAGD = "GD003", SOTK = "TK0000002", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-7), SOTIEN = 1000000, MANV = "NV00000002", Status = "Completed" },
        new Transaction { MAGD = "GD004", SOTK = "TK0000003", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-3), SOTIEN = 3000000, MANV = "NV00000003", Status = "Completed" },
        new Transaction { MAGD = "GD005", SOTK = "TK0000005", LOAIGD = "GT", NGAYGD = DateTime.Now.AddDays(-1), SOTIEN = 500000, MANV = "NV00000001", Status = "Completed" }
    };

    private readonly IAccountRepository _accountRepository;
    private int _nextId = 6;

    public InMemoryTransactionRepository(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
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
        var accounts = await _accountRepository.GetAccountsByBranchAsync(branchCode);
        var accountNumbers = accounts.Select(a => a.SOTK).ToList();

        var transactions = _transactions
            .Where(t => accountNumbers.Contains(t.SOTK))
            .Where(t => !fromDate.HasValue || t.NGAYGD >= fromDate.Value)
            .Where(t => !toDate.HasValue || t.NGAYGD <= toDate.Value)
            .OrderByDescending(t => t.NGAYGD)
            .ToList();

        return transactions;
    }

    public Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date)
    {
        var total = _transactions
            .Where(t => t.SOTK == accountNumber)
            .Where(t => t.LOAIGD == "RT") // Withdrawals
            .Where(t => t.NGAYGD.Date == date.Date)
            .Where(t => t.Status == "Completed") // Only count completed transactions
            .Sum(t => t.SOTIEN);

        return Task.FromResult(total);
    }

    public Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date)
    {
        // Count outgoing transfers only (sender side). Normalise "CK" legacy code to "CT".
        var total = _transactions
            .Where(t => t.SOTK == accountNumber)
            .Where(t => t.LOAIGD == "CT" || t.LOAIGD == "CK")
            .Where(t => t.NGAYGD.Date == date.Date)
            .Where(t => t.Status == "Completed")
            .Sum(t => t.SOTIEN);

        return Task.FromResult(total);
    }

    public async Task<bool> DepositAsync(string sotk, decimal amount, string manv)
    {
        var transaction = new Transaction
        {
            MAGD = $"GD{_nextId++:D3}",
            SOTK = sotk,
            LOAIGD = "GT",
            NGAYGD = DateTime.Now,
            SOTIEN = amount,
            MANV = manv,
            Status = "Pending"
        };

        try
        {
            if (amount <= 0)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Amount must be greater than 0";
                _transactions.Add(transaction);
                return false;
            }

            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Account not found";
                _transactions.Add(transaction);
                return false;
            }

            if (account.Status == "Closed")
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Cannot perform transactions on closed account";
                _transactions.Add(transaction);
                return false;
            }

            account.SODU += amount;
            await _accountRepository.UpdateAccountAsync(account);

            transaction.Status = "Completed";
            _transactions.Add(transaction);
            return true;
        }
        catch (Exception ex)
        {
            transaction.Status = "Failed";
            transaction.ErrorMessage = ex.Message;
            _transactions.Add(transaction);
            return false;
        }
    }

    public async Task<bool> WithdrawAsync(string sotk, decimal amount, string manv)
    {
        var transaction = new Transaction
        {
            MAGD = $"GD{_nextId++:D3}",
            SOTK = sotk,
            LOAIGD = "RT",
            NGAYGD = DateTime.Now,
            SOTIEN = amount,
            MANV = manv,
            Status = "Pending"
        };

        try
        {
            if (amount <= 0)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Amount must be greater than 0";
                _transactions.Add(transaction);
                return false;
            }

            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Account not found";
                _transactions.Add(transaction);
                return false;
            }

            if (account.Status == "Closed")
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Cannot perform transactions on closed account";
                _transactions.Add(transaction);
                return false;
            }

            if (account.SODU < amount)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = "Insufficient balance";
                _transactions.Add(transaction);
                return false;
            }

            account.SODU -= amount;
            await _accountRepository.UpdateAccountAsync(account);

            transaction.Status = "Completed";
            _transactions.Add(transaction);
            return true;
        }
        catch (Exception ex)
        {
            transaction.Status = "Failed";
            transaction.ErrorMessage = ex.Message;
            _transactions.Add(transaction);
            return false;
        }
    }

    public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
    {
        var now  = DateTime.Now;
        var magd = $"GD{_nextId++:D3}";

        // Records a failed audit entry and throws so the caller gets a meaningful message.
        void Fail(string reason)
        {
            _transactions.Add(new Transaction
            {
                MAGD      = magd,
                SOTK      = sotkFrom,
                LOAIGD    = "CK",
                NGAYGD    = now,
                SOTIEN    = amount,
                MANV      = manv,
                SOTK_NHAN = sotkTo,
                Status       = "Failed",
                ErrorMessage = reason
            });
            throw new InvalidOperationException(reason);
        }

        // ── Pre-validation (outside lock — fast-path error messages) ──────────

        if (amount <= 0)
            Fail("Transfer amount must be greater than 0.");

        if (sotkFrom == sotkTo)
            Fail("Cannot transfer to the same account.");

        var accountFrom = await _accountRepository.GetAccountAsync(sotkFrom);
        if (accountFrom == null)
            Fail($"Source account '{sotkFrom}' not found.");

        if (accountFrom!.Status == "Closed")
            Fail("Cannot transfer from a closed account.");

        if (accountFrom.SODU < amount)
            Fail($"Insufficient balance. Available: {accountFrom.SODU:N0} VND, requested: {amount:N0} VND.");

        var accountTo = await _accountRepository.GetAccountAsync(sotkTo);
        if (accountTo == null)
            Fail($"Destination account '{sotkTo}' not found.");

        if (accountTo!.Status == "Closed")
            Fail("Cannot transfer to a closed account.");

        // ── Atomic transfer ────────────────────────────────────────────────────
        // AtomicTransferAsync re-validates ALL conditions inside the lock,
        // guarding against concurrent withdrawals or account-close operations
        // that may have changed state between the pre-validation reads above
        // and the lock acquisition here.
        var success = await _accountRepository.AtomicTransferAsync(sotkFrom, sotkTo, amount);

        if (!success)
        {
            // Only reached when a race condition changed state between pre-validation
            // and the lock. Balance or status was modified concurrently.
            Fail("Transfer failed: the account state changed concurrently. Please retry.");
        }

        // ── Record completed transaction ───────────────────────────────────────
        _transactions.Add(new Transaction
        {
            MAGD      = magd,
            SOTK      = sotkFrom,
            LOAIGD    = "CK",
            NGAYGD    = now,
            SOTIEN    = amount,
            MANV      = manv,
            SOTK_NHAN = sotkTo,
            Status    = "Completed"
        });

        return true;
    }
}
