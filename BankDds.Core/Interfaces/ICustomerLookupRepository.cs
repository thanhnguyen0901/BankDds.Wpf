using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository for read-only customer lookup across all branches.
/// Data source: NGANHANG_TRACUU subscriber (KHACHHANG + CHINHANH replicated
/// from both CN1 and CN2 without row filters).
/// </summary>
public interface ICustomerLookupRepository
{
    /// <summary>
    /// Look up a single customer by exact CMND (ID card number).
    /// Returns null when no match is found.
    /// </summary>
    Task<Customer?> GetCustomerByCmndAsync(string cmnd);

    /// <summary>
    /// Search customers whose name (HO + TEN) contains <paramref name="keyword"/>.
    /// Returns at most <paramref name="maxResults"/> rows to prevent full-table scans.
    /// </summary>
    Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50);
}
