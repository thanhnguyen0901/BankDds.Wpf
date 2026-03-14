using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines user management business operations for administration screens.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets user profile by login name.
        /// </summary>
        /// <param name="username">Login name.</param>
        /// <returns>The user profile when found; otherwise null.</returns>
        Task<User?> GetUserAsync(string username);

        /// <summary>
        /// Creates a new application user and login mapping.
        /// </summary>
        /// <param name="user">User entity.</param>
        /// <returns>True when a user login and profile mapping are created successfully; otherwise false.</returns>
        Task<bool> AddUserAsync(User user);

        /// <summary>
        /// Updates user credentials or profile mapping.
        /// </summary>
        /// <param name="user">User entity.</param>
        /// <returns>True when user profile mapping is updated successfully; otherwise false.</returns>
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// Soft-deletes a user and removes login if needed.
        /// </summary>
        /// <param name="username">Login name.</param>
        /// <returns>True when the user is soft-deleted successfully; otherwise false.</returns>
        Task<bool> DeleteUserAsync(string username);

        /// <summary>
        /// Restores a previously deleted user.
        /// </summary>
        /// <param name="username">Login name.</param>
        /// <returns>True when a deleted user is restored successfully; otherwise false.</returns>
        Task<bool> RestoreUserAsync(string username);

        /// <summary>
        /// Gets all active and inactive users for administration management.
        /// </summary>
        /// <returns>A list of users available for administration management.</returns>
        Task<List<User>> GetAllUsersAsync();

    }
}
