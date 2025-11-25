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

    public string GetConnectionStringForBranch(string branch)
    {
        var key = $"ConnectionStrings:Branch_{branch}";
        return _configuration[key] ?? throw new InvalidOperationException($"Connection string not found for branch: {branch}");
    }

    public string GetBankConnection()
    {
        return _configuration["ConnectionStrings:Bank_Main"] ?? throw new InvalidOperationException("Bank main connection string not found");
    }

    public string DefaultBranch => _configuration["DatabaseSettings:DefaultBranch"] ?? "BENTHANH";
}
