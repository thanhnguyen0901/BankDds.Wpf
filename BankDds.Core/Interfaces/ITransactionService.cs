using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface ITransactionService
{
    Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk);
    Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null);
    Task<bool> DepositAsync(string sotk, decimal amount, int manv);
    Task<bool> WithdrawAsync(string sotk, decimal amount, int manv);
    Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, int manv);
    
    // New methods for daily limit tracking
    Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date);
    Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date);
}
