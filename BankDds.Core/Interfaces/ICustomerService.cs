using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines customer business operations with branch-level constraints.
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Gets customers that belong to a specific branch.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A list of customers that belong to the requested branch.</returns>
        Task<List<Customer>> GetCustomersByBranchAsync(string branchCode);

        /// <summary>
        /// Gets all customers available in the current data scope.
        /// </summary>
        /// <returns>A list of customers available in the current data scope.</returns>
        Task<List<Customer>> GetAllCustomersAsync();

        /// <summary>
        /// Gets a customer by CMND from lookup source.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>The customer profile when found; otherwise null.</returns>
        Task<Customer?> GetCustomerByCMNDAsync(string cmnd);

        /// <summary>
        /// Gets a customer by CMND from a branch-local source for branch-scoped mutate workflows.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>The customer profile when found in the requested branch shard; otherwise null.</returns>
        Task<Customer?> GetCustomerByCMNDFromBranchAsync(string cmnd, string branchCode);

        /// <summary>
        /// Creates a new customer profile.
        /// </summary>
        /// <param name="customer">Customer entity.</param>
        /// <returns>True when a customer profile is created successfully; otherwise false.</returns>
        Task<bool> AddCustomerAsync(Customer customer);

        /// <summary>
        /// Updates customer profile information.
        /// </summary>
        /// <param name="customer">Customer entity.</param>
        /// <returns>True when customer information is updated successfully; otherwise false.</returns>
        Task<bool> UpdateCustomerAsync(Customer customer);

        /// <summary>
        /// Soft-deletes a customer profile.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>True when the customer is marked as deleted successfully; otherwise false.</returns>
        Task<bool> DeleteCustomerAsync(string cmnd);

        /// <summary>
        /// Restores a previously deleted customer profile.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>True when a deleted customer is restored successfully; otherwise false.</returns>
        Task<bool> RestoreCustomerAsync(string cmnd);

    }
}
