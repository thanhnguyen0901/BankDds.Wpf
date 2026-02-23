using BankDds.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BankDds.Infrastructure.Configuration;

/// <summary>
/// Provides ADO.NET connection strings for the Banking distributed topology.
/// <para>
/// Connection strings in appsettings contain Server + Database + TrustServerCertificate
/// but NO credentials.  After login, <see cref="SetSqlLoginCredentials"/> injects
/// User Id / Password so every subsequent DB call runs under that SQL login.
/// </para>
/// </summary>
public class ConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _configuration;
    private string? _sqlLogin;
    private string? _sqlPassword;

    public ConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ───────────────────────── credential management ─────────────────────────

    /// <inheritdoc />
    public void SetSqlLoginCredentials(string sqlLogin, string sqlPassword)
    {
        _sqlLogin = sqlLogin ?? throw new ArgumentNullException(nameof(sqlLogin));
        _sqlPassword = sqlPassword ?? throw new ArgumentNullException(nameof(sqlPassword));
    }

    /// <inheritdoc />
    public void ClearSqlLoginCredentials()
    {
        _sqlLogin = null;
        _sqlPassword = null;
    }

    // ───────────────────────── Publisher ─────────────────────────

    /// <inheritdoc />
    public string GetPublisherConnection()
    {
        var template = GetPublisherTemplate();
        if (_sqlLogin is null || _sqlPassword is null)
            throw new InvalidOperationException(
                "SQL login credentials have not been set. Call SetSqlLoginCredentials first.");

        return InjectCredentials(template, _sqlLogin, _sqlPassword);
    }

    /// <inheritdoc />
    public string GetPublisherConnectionForLogin(string sqlLogin, string sqlPassword)
    {
        var template = GetPublisherTemplate();
        return InjectCredentials(template, sqlLogin, sqlPassword);
    }

    /// <inheritdoc />
    [Obsolete("Use GetPublisherConnection() instead.")]
    public string GetBankConnection() => GetPublisherConnection();

    // ───────────────────────── Branch subscriber ─────────────────────────

    /// <inheritdoc />
    public string GetConnectionStringForBranch(string branch)
    {
        var key = $"ConnectionStrings:Branch_{branch}";
        var template = _configuration[key]
            ?? throw new InvalidOperationException(
                $"Connection string not found for branch: {branch}");

        if (_sqlLogin is null || _sqlPassword is null)
            throw new InvalidOperationException(
                "SQL login credentials have not been set. Call SetSqlLoginCredentials first.");

        return InjectCredentials(template, _sqlLogin, _sqlPassword);
    }

    // ───────────────────────── misc ─────────────────────────

    public string DefaultBranch =>
        _configuration["DatabaseSettings:DefaultBranch"] ?? "BENTHANH";

    // ───────────────────────── helpers ─────────────────────────

    private string GetPublisherTemplate()
    {
        return _configuration["ConnectionStrings:Publisher"]
            ?? throw new InvalidOperationException(
                "Publisher connection string not found in configuration.");
    }

    /// <summary>
    /// Merges User Id and Password into the template connection string
    /// using <see cref="SqlConnectionStringBuilder"/> so the result is always valid.
    /// </summary>
    private static string InjectCredentials(string template, string login, string password)
    {
        var builder = new SqlConnectionStringBuilder(template)
        {
            UserID = login,
            Password = password
        };
        return builder.ConnectionString;
    }
}
