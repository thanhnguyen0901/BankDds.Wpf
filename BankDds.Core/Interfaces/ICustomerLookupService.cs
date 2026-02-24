using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Service layer for cross-branch customer lookup via the lookup subscriber.
/// Only NGANHANG-role users should be routed here.
/// </summary>
public interface ICustomerLookupService
{
    /// <summary>Look up a customer by exact CMND across all branches.</summary>
    Task<Customer?> GetCustomerByCmndAsync(string cmnd);

    /// <summary>Search customers by name keyword across all branches.</summary>
    Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50);
}
