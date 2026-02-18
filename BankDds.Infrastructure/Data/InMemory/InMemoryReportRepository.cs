using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

public class InMemoryReportRepository : IReportRepository
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICustomerRepository _customerRepository;

    public InMemoryReportRepository(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ICustomerRepository customerRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
    }

    public async Task<AccountStatement?> GetAccountStatementAsync(string accountNumber, DateTime fromDate, DateTime toDate)
    {
        // ── Input validation ───────────────────────────────────────────────────
        if (fromDate.Date > toDate.Date)
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");

        var account = await _accountRepository.GetAccountAsync(accountNumber);
        if (account == null)
            return null;

        // Retrieve every transaction that touches this account (as sender or receiver).
        // The repository returns both roles already (SOTK == account OR SOTK_NHAN == account).
        var allTransactions = await _transactionRepository.GetTransactionsByAccountAsync(accountNumber);

        // ── Normalise "CK" → "CT" without mutating the cached objects ─────────
        // Work on a projected copy so the in-memory cache stays clean.
        var normalised = allTransactions
            .Where(t => t.Status == "Completed")
            .Select(t => (
                t.MAGD,
                t.SOTK,
                LOAIGD: t.LOAIGD == "CK" ? "CT" : t.LOAIGD,
                t.NGAYGD,
                t.SOTIEN,
                t.SOTK_NHAN
            ))
            .OrderBy(t => t.NGAYGD)
            .ToList();

        // ── Compute opening balance = balance just before the start of fromDate ──
        // Strategy: start from the current account balance and subtract (undo) every
        // completed transaction that falls on or after fromDate (inclusive).
        // "Undo" means reversing the effect each transaction had on THIS account.
        decimal openingBalance = account.SODU;

        var txAfterFrom = normalised.Where(t => t.NGAYGD.Date >= fromDate.Date).ToList();

        foreach (var t in txAfterFrom)
        {
            if (t.SOTK == accountNumber)
            {
                // This account was the originator.
                if (t.LOAIGD == "GT")
                    openingBalance -= t.SOTIEN;   // deposit increased balance → undo by subtracting
                else // RT or CT (sender)
                    openingBalance += t.SOTIEN;   // withdrawal/transfer decreased balance → undo by adding
            }
            else
            {
                // This account was the receiver of a CT transfer.
                openingBalance -= t.SOTIEN;       // receiving increased balance → undo by subtracting
            }
        }

        // ── Build statement lines for the requested period ─────────────────────
        // toDate is treated as end-of-day inclusive.
        var periodTx = normalised
            .Where(t => t.NGAYGD.Date >= fromDate.Date && t.NGAYGD.Date <= toDate.Date)
            .ToList(); // already sorted ASC

        var lines     = new List<StatementLine>();
        decimal running = openingBalance;

        foreach (var t in periodTx)
        {
            decimal before = running;
            bool    isDebit;
            string  description;

            if (t.SOTK == accountNumber)
            {
                if (t.LOAIGD == "GT")
                {
                    running    += t.SOTIEN;
                    isDebit     = false;
                    description = "Gửi tiền";
                }
                else if (t.LOAIGD == "RT")
                {
                    running    -= t.SOTIEN;
                    isDebit     = true;
                    description = "Rút tiền";
                }
                else // CT — sender
                {
                    running    -= t.SOTIEN;
                    isDebit     = true;
                    description = $"Chuyển tiền đến {t.SOTK_NHAN}";
                }
            }
            else
            {
                // Receiver of a CT transfer
                running    += t.SOTIEN;
                isDebit     = false;
                description = $"Nhận tiền từ {t.SOTK}";
            }

            lines.Add(new StatementLine
            {
                OpeningBalance  = before,
                Date            = t.NGAYGD,
                TransactionType = t.LOAIGD == "CT" && t.SOTK_NHAN == accountNumber ? "CT" : t.LOAIGD,
                TransactionId   = t.MAGD,
                Amount          = t.SOTIEN,
                RunningBalance  = running,
                Description     = description,
                IsDebit         = isDebit
            });
        }

        return new AccountStatement
        {
            SOTK           = accountNumber,
            FromDate       = fromDate.Date,
            ToDate         = toDate.Date,
            OpeningBalance = openingBalance,
            Lines          = lines,
            ClosingBalance = running
        };
    }

    public async Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
    {
        var allAccounts = await _accountRepository.GetAllAccountsAsync();
        var filtered = allAccounts.Where(a => a.NGAYMOTK >= fromDate && a.NGAYMOTK <= toDate);
        
        if (!string.IsNullOrEmpty(branchCode) && branchCode != "ALL")
        {
            filtered = filtered.Where(a => a.MACN == branchCode);
        }
        
        return filtered.OrderBy(a => a.NGAYMOTK).ToList();
    }

    public async Task<List<Customer>> GetCustomersByBranchAsync(string? branchCode = null)
    {
        var customers = await _customerRepository.GetAllCustomersAsync();
        
        if (!string.IsNullOrEmpty(branchCode) && branchCode != "ALL")
        {
            customers = customers.Where(c => c.MaCN == branchCode).ToList();
        }
        
        return customers.OrderBy(c => c.FullName).ToList();
    }

    public async Task<TransactionSummary?> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
    {
        List<Transaction> transactions;
        
        if (string.IsNullOrEmpty(branchCode) || branchCode == "ALL")
        {
            var allAccounts = await _accountRepository.GetAllAccountsAsync();
            var allTransactions = new List<Transaction>();
            
            foreach (var account in allAccounts)
            {
                var accountTransactions = await _transactionRepository.GetTransactionsByAccountAsync(account.SOTK);
                allTransactions.AddRange(accountTransactions);
            }
            
            transactions = allTransactions
                .Where(t => t.NGAYGD >= fromDate && t.NGAYGD <= toDate)
                .OrderByDescending(t => t.NGAYGD)
                .ToList();
        }
        else
        {
            transactions = await _transactionRepository.GetTransactionsByBranchAsync(branchCode, fromDate, toDate);
        }

        var uniqueTransactions = transactions
            .GroupBy(t => t.MAGD)
            .Select(g => g.First())
            .ToList();

        return new TransactionSummary
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchCode = branchCode,
            Transactions = uniqueTransactions,
            TotalTransactionCount = uniqueTransactions.Count,
            DepositCount = uniqueTransactions.Count(t => t.LOAIGD == "GT"),
            WithdrawalCount = uniqueTransactions.Count(t => t.LOAIGD == "RT"),
            TransferCount = uniqueTransactions.Count(t => t.LOAIGD == "CK" || t.LOAIGD == "CT"),
            TotalDepositAmount = uniqueTransactions.Where(t => t.LOAIGD == "GT").Sum(t => t.SOTIEN),
            TotalWithdrawalAmount = uniqueTransactions.Where(t => t.LOAIGD == "RT").Sum(t => t.SOTIEN),
            TotalTransferAmount = uniqueTransactions.Where(t => t.LOAIGD == "CK" || t.LOAIGD == "CT").Sum(t => t.SOTIEN)
        };
    }
}
