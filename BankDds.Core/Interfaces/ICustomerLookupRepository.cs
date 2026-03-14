using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines read-only lookup operations for customer search across branches.
    /// </summary>
    public interface ICustomerLookupRepository
    {
        /// <summary>
        /// Gets a customer by CMND from lookup source.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>The customer profile when found; otherwise null.</returns>
        Task<Customer?> GetCustomerByCmndAsync(string cmnd);

        /// <summary>
        /// Searches customers by name keyword across lookup scope.
        /// </summary>
        /// <param name="keyword">Search keyword.</param>
        /// <param name="maxResults">Maximum number of returned records.</param>
        /// <returns>A list of customers whose names match the search keyword.</returns>
        Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50);

    }
}
