namespace BankDds.Core.Interfaces;

/// <summary>
/// Provides ADO.NET connection strings for the Banking distributed topology.
/// <para>
/// After login, <see cref="SetSqlLoginCredentials"/> injects the SQL login/password
/// into all connection strings so that every DB call runs under the logged-in user's
/// permissions (NGANHANG / CHINHANH / KHACHHANG role).
/// </para>
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>
    /// Connection string for a branch subscriber database.
    /// After <see cref="SetSqlLoginCredentials"/> is called, uses the SQL login credentials.
    /// </summary>
    string GetConnectionStringForBranch(string branch);

    /// <summary>
    /// Connection string for the Publisher database (NGANHANG_PUB).
    /// Used for: login authentication, aggregate reports, branch listing.
    /// </summary>
    string GetPublisherConnection();

    /// <summary>
    /// Returns the Publisher connection string.
    /// Kept for backward compatibility — delegates to <see cref="GetPublisherConnection"/>.
    /// </summary>
    [System.Obsolete("Use GetPublisherConnection() instead.")]
    string GetBankConnection();

    /// <summary>
    /// Builds a Publisher connection string using explicit SQL login credentials.
    /// Used during the login phase (before SetSqlLoginCredentials is called).
    /// </summary>
    string GetPublisherConnectionForLogin(string sqlLogin, string sqlPassword);

    /// <summary>
    /// Stores the SQL login credentials from the login form.
    /// All subsequent connection strings will embed these credentials.
    /// </summary>
    void SetSqlLoginCredentials(string sqlLogin, string sqlPassword);

    /// <summary>Clears stored SQL login credentials (on logout).</summary>
    void ClearSqlLoginCredentials();

    /// <summary>
    /// Connection string for the read-only lookup subscriber database
    /// (NGANHANG_TRACUU on SQLSERVER4). Contains KHACHHANG + CHINHANH replicated
    /// from all branches. Returns null when the key is not configured.
    /// </summary>
    string? GetLookupConnection();

    /// <summary>
    /// Returns branch codes configured via ConnectionStrings:Branch_&lt;MACN&gt; keys.
    /// Used pre-login to populate branch selection without opening a DB connection.
    /// </summary>
    IReadOnlyList<string> GetConfiguredBranchCodes();

    string DefaultBranch { get; }
}
