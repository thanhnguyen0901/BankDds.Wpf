using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetCustomersByBranchAsync(string branchCode);
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByCMNDAsync(string cmnd);
    Task<bool> AddCustomerAsync(Customer customer);
    Task<bool> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(string cmnd);
    Task<bool> RestoreCustomerAsync(string cmnd);
}
