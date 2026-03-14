namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines connection string resolution for publisher, branch, and lookup databases.
    /// </summary>
    public interface IConnectionStringProvider
    {
        /// <summary>
        /// Gets connection string for the requested branch database.
        /// </summary>
        /// <param name="branch">Business branch code used to resolve branch database connection.</param>
        /// <returns>A connection string for the selected branch database.</returns>
        string GetConnectionStringForBranch(string branch);

        /// <summary>
        /// Gets connection string for publisher database.
        /// </summary>
        /// <returns>A connection string for the publisher database.</returns>
        string GetPublisherConnection();

        /// <summary>
        /// Gets publisher connection string by legacy method name.
        /// </summary>
        /// <returns>A connection string for the publisher database (legacy alias).</returns>
        [System.Obsolete("Use GetPublisherConnection() instead.")]
        string GetBankConnection();

        /// <summary>
        /// Builds publisher connection string using provided SQL credentials.
        /// </summary>
        /// <param name="sqlLogin">SQL login name.</param>
        /// <param name="sqlPassword">SQL login password.</param>
        /// <returns>A publisher connection string built with provided SQL credentials.</returns>
        string GetPublisherConnectionForLogin(string sqlLogin, string sqlPassword);

        /// <summary>
        /// Stores runtime SQL credentials for subsequent connections.
        /// </summary>
        /// <param name="sqlLogin">SQL login name.</param>
        /// <param name="sqlPassword">SQL login password.</param>
        void SetSqlLoginCredentials(string sqlLogin, string sqlPassword);

        /// <summary>
        /// Clears runtime SQL credentials from memory.
        /// </summary>
        void ClearSqlLoginCredentials();

        /// <summary>
        /// Gets connection string for global lookup database.
        /// </summary>
        /// <returns>A connection string for the lookup database, or null when lookup is disabled.</returns>
        string? GetLookupConnection();

        /// <summary>
        /// Gets branch codes configured in application settings.
        /// </summary>
        /// <returns>A read-only list of configured branch codes in application settings.</returns>
        IReadOnlyList<string> GetConfiguredBranchCodes();

        string DefaultBranch { get; }
    }
}
