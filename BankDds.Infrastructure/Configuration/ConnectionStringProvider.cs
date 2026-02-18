using BankDds.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BankDds.Infrastructure.Configuration;

public class ConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    public ConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the ADO.NET connection string for a branch server.
    /// Override priority:
    ///   1. Environment variable  BANKDDS_CONNSTR_{BRANCH}  (e.g. BANKDDS_CONNSTR_BENTHANH)
    ///   2. Standard .NET env var ConnectionStrings__Branch_{branch}  (set via AddEnvironmentVariables)
    ///   3. appsettings.Development.json  (git-ignored local file)
    ///   4. appsettings.json  (placeholder value — never a real password)
    /// </summary>
    public string GetConnectionStringForBranch(string branch)
    {
        // Explicit short-form env var (developer-friendly alternative to the standard form)
        var envVar = Environment.GetEnvironmentVariable(
            $"BANKDDS_CONNSTR_{branch.ToUpperInvariant()}");
        if (!string.IsNullOrWhiteSpace(envVar))
            return envVar;

        var key = $"ConnectionStrings:Branch_{branch}";
        return _configuration[key]
            ?? throw new InvalidOperationException($"Connection string not found for branch: {branch}");
    }

    /// <summary>
    /// Returns the ADO.NET connection string for the central Bank_Main database.
    /// Override priority: BANKDDS_CONNSTR_BANK_MAIN env var → appsettings.Development.json → appsettings.json.
    /// </summary>
    public string GetBankConnection()
    {
        var envVar = Environment.GetEnvironmentVariable("BANKDDS_CONNSTR_BANK_MAIN");
        if (!string.IsNullOrWhiteSpace(envVar))
            return envVar;

        return _configuration["ConnectionStrings:Bank_Main"]
            ?? throw new InvalidOperationException("Bank main connection string not found");
    }

    public string DefaultBranch => _configuration["DatabaseSettings:DefaultBranch"] ?? "BENTHANH";
}
