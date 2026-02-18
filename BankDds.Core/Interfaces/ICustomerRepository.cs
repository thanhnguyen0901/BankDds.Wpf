using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository interface for Customer data access operations
/// </summary>
public interface ICustomerRepository
{
    Task<List<Customer>> GetCustomersByBranchAsync(string branchCode);
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByCMNDAsync(string cmnd);
    Task<bool> AddCustomerAsync(Customer customer);
    Task<bool> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(string cmnd);
    Task<bool> RestoreCustomerAsync(string cmnd);
}
