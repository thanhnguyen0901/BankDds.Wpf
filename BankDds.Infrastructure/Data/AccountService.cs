using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// Account service that delegates to IAccountRepository for data access with authorization
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuthorizationService _authorizationService;

    public AccountService(
        IAccountRepository accountRepository, 
        ICustomerRepository customerRepository,
        IAuthorizationService authorizationService)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _authorizationService = authorizationService;
    }

    public Task<List<Account>> GetAccountsByBranchAsync(string branchCode)
    {
        // Verify user can access this branch
        _authorizationService.RequireCanAccessBranch(branchCode);
        return _accountRepository.GetAccountsByBranchAsync(branchCode);
    }

    public Task<List<Account>> GetAllAccountsAsync()
    {
        // Only NganHang can get all accounts
        if (!_authorizationService.CanAccessBranch("ALL"))
        {
            throw new UnauthorizedAccessException("Only bank-level users can access all accounts.");
        }
        return _accountRepository.GetAllAccountsAsync();
    }

    public async Task<List<Account>> GetAccountsByCustomerAsync(string cmnd)
    {
        // Verify user can access this customer
        _authorizationService.RequireCanAccessCustomer(cmnd);
        
        // Get customer to verify branch access
        var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
        if (customer != null)
        {
            _authorizationService.RequireCanAccessBranch(customer.MaCN);
        }
        
        return await _accountRepository.GetAccountsByCustomerAsync(cmnd);
    }

    public async Task<Account?> GetAccountAsync(string sotk)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            return null;

        // Verify user can access this account's customer and branch
        _authorizationService.RequireCanAccessAccount(account.CMND);
        _authorizationService.RequireCanAccessBranch(account.MACN);
        
        return account;
    }

    public async Task<bool> AddAccountAsync(Account account)
    {
        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(account.MACN);
        
        // Verify customer exists and user can access
        var customer = await _customerRepository.GetCustomerByCMNDAsync(account.CMND);
        if (customer == null)
            throw new InvalidOperationException("Customer not found");
        
        _authorizationService.RequireCanAccessCustomer(account.CMND);
        
        return await _accountRepository.AddAccountAsync(account);
    }

    public async Task<bool> UpdateAccountAsync(Account account)
    {
        var existing = await _accountRepository.GetAccountAsync(account.SOTK);
        if (existing == null)
            return false;

        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(existing.MACN);
        
        return await _accountRepository.UpdateAccountAsync(account);
    }

    public async Task<bool> DeleteAccountAsync(string sotk)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            return false;

        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(account.MACN);
        
        return await _accountRepository.DeleteAccountAsync(sotk);
    }

    public async Task<bool> CloseAccountAsync(string sotk)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            return false;

        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(account.MACN);
        
        return await _accountRepository.CloseAccountAsync(sotk);
    }

    public async Task<bool> ReopenAccountAsync(string sotk)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            return false;

        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(account.MACN);
        
        return await _accountRepository.ReopenAccountAsync(sotk);
    }
}
