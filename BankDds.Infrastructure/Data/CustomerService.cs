using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// Customer service that delegates to ICustomerRepository for data access with authorization
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuthorizationService _authorizationService;

    public CustomerService(ICustomerRepository customerRepository, IAuthorizationService authorizationService)
    {
        _customerRepository = customerRepository;
        _authorizationService = authorizationService;
    }

    public Task<List<Customer>> GetCustomersByBranchAsync(string branchCode)
    {
        // Verify user can access this branch
        _authorizationService.RequireCanAccessBranch(branchCode);
        return _customerRepository.GetCustomersByBranchAsync(branchCode);
    }

    public Task<List<Customer>> GetAllCustomersAsync()
    {
        // Only NganHang can get all customers across branches
        if (!_authorizationService.CanAccessBranch("ALL"))
        {
            throw new UnauthorizedAccessException("Only bank-level users can access all customers.");
        }
        return _customerRepository.GetAllCustomersAsync();
    }

    public async Task<Customer?> GetCustomerByCMNDAsync(string cmnd)
    {
        // Get customer first to check branch
        var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
        if (customer == null)
            return null;

        // Verify authorization
        _authorizationService.RequireCanAccessCustomer(cmnd);
        _authorizationService.RequireCanAccessBranch(customer.MaCN);
        
        return customer;
    }

    public async Task<bool> AddCustomerAsync(Customer customer)
    {
        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(customer.MaCN);
        
        return await _customerRepository.AddCustomerAsync(customer);
    }

    public async Task<bool> UpdateCustomerAsync(Customer customer)
    {
        // Get existing customer to verify branch access
        var existing = await _customerRepository.GetCustomerByCMNDAsync(customer.CMND);
        if (existing == null)
            return false;

        // Verify user can modify both old and new branch
        _authorizationService.RequireCanModifyBranch(existing.MaCN);
        _authorizationService.RequireCanModifyBranch(customer.MaCN);
        
        return await _customerRepository.UpdateCustomerAsync(customer);
    }

    public async Task<bool> DeleteCustomerAsync(string cmnd)
    {
        // Get customer to check branch
        var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
        if (customer == null)
            return false;

        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(customer.MaCN);
        
        return await _customerRepository.DeleteCustomerAsync(cmnd);
    }

    public async Task<bool> RestoreCustomerAsync(string cmnd)
    {
        // Get customer to check branch
        var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
        if (customer == null)
            return false;

        // Verify user can modify this branch
        _authorizationService.RequireCanModifyBranch(customer.MaCN);
        
        return await _customerRepository.RestoreCustomerAsync(cmnd);
    }
}
