using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IAccountService
{
    Task<List<Account>> GetAccountsByBranchAsync(string branchCode);
    Task<List<Account>> GetAllAccountsAsync();
    Task<List<Account>> GetAccountsByCustomerAsync(string cmnd);
    Task<Account?> GetAccountAsync(string sotk);
    Task<bool> AddAccountAsync(Account account);
    Task<bool> UpdateAccountAsync(Account account);
    Task<bool> DeleteAccountAsync(string sotk);
    Task<bool> CloseAccountAsync(string sotk);
    Task<bool> ReopenAccountAsync(string sotk);
}
