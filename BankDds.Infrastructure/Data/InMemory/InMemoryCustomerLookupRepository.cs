using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory stub for <see cref="ICustomerLookupRepository"/>.
/// Used when DataMode = InMemory (no SQL Server available).
/// Returns empty results — customer lookup is a SQL-only feature.
/// </summary>
public class InMemoryCustomerLookupRepository : ICustomerLookupRepository
{
    public Task<Customer?> GetCustomerByCmndAsync(string cmnd)
        => Task.FromResult<Customer?>(null);

    public Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50)
        => Task.FromResult(new List<Customer>());
}
