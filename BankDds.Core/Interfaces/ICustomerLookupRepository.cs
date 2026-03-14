using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    public interface ICustomerLookupRepository
    {
        Task<Customer?> GetCustomerByCmndAsync(string cmnd);
        Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50);
    }
}