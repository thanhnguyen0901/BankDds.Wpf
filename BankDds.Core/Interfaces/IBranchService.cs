using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines branch management operations in business layer.
    /// </summary>
    public interface IBranchService
    {
        /// <summary>
        /// Gets all branch master records.
        /// </summary>
        /// <returns>A list of all branches in the banking system.</returns>
        Task<List<Branch>> GetAllBranchesAsync();

        /// <summary>
        /// Gets branch details by branch code.
        /// </summary>
        /// <param name="macn">Branch code.</param>
        /// <returns>The branch details when found; otherwise null.</returns>
        Task<Branch?> GetBranchAsync(string macn);

        /// <summary>
        /// Creates a new branch record.
        /// </summary>
        /// <param name="branch">Branch entity that will be inserted.</param>
        /// <returns>True when a new branch is created successfully; otherwise false.</returns>
        Task<bool> AddBranchAsync(Branch branch);

        /// <summary>
        /// Updates an existing branch record.
        /// </summary>
        /// <param name="branch">Branch entity that will be updated.</param>
        /// <returns>True when branch information is updated successfully; otherwise false.</returns>
        Task<bool> UpdateBranchAsync(Branch branch);

        /// <summary>
        /// Deletes a branch record.
        /// </summary>
        /// <param name="macn">Branch code.</param>
        /// <returns>True when the branch is deleted successfully; otherwise false.</returns>
        Task<bool> DeleteBranchAsync(string macn);

        /// <summary>
        /// Determines whether the specified branch code already exists.
        /// </summary>
        /// <param name="macn">Branch code.</param>
        /// <returns>True when the specified branch code exists; otherwise false.</returns>
        Task<bool> BranchExistsAsync(string macn);

    }
}
