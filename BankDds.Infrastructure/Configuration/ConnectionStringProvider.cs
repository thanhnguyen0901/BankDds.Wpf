using BankDds.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BankDds.Infrastructure.Configuration
{
    /// <summary>
    /// Provides SQL Server connection strings for publisher, branch shards, and lookup database.
    /// </summary>
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IConfiguration _configuration;
        private string? _sqlLogin;
        private string? _sqlPassword;

        /// <summary>
        /// Initializes provider with application configuration source.
        /// </summary>
        /// <param name="configuration">Application configuration source.</param>
        public ConnectionStringProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SetSqlLoginCredentials(string sqlLogin, string sqlPassword)
        {
            _sqlLogin = sqlLogin ?? throw new ArgumentNullException(nameof(sqlLogin));
            _sqlPassword = sqlPassword ?? throw new ArgumentNullException(nameof(sqlPassword));
        }

        public void ClearSqlLoginCredentials()
        {
            _sqlLogin = null;
            _sqlPassword = null;
        }

        public string GetPublisherConnection()
        {
            var template = GetPublisherTemplate();

            // Logic: runtime SQL credentials are mandatory because login flow can switch user identity.
            if (_sqlLogin is null || _sqlPassword is null)
            {
                throw new InvalidOperationException(
                    "Chưa thiết lập thông tin đăng nhập SQL. Hãy gọi SetSqlLoginCredentials trước.");
            }

            return InjectCredentials(template, _sqlLogin, _sqlPassword);
        }

        public string GetPublisherConnectionForLogin(string sqlLogin, string sqlPassword)
        {
            var template = GetPublisherTemplate();
            return InjectCredentials(template, sqlLogin, sqlPassword);
        }

        [Obsolete("Use GetPublisherConnection() instead.")]
        public string GetBankConnection() => GetPublisherConnection();

        public string GetConnectionStringForBranch(string branch)
        {
            var normalizedBranch = (branch ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(normalizedBranch))
            {
                throw new InvalidOperationException("Mã chi nhánh không được để trống khi lấy chuỗi kết nối.");
            }

            var key = $"ConnectionStrings:Branch_{normalizedBranch}";
            var template = _configuration[key]
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy chuỗi kết nối cho chi nhánh: {normalizedBranch}");

            // Logic: branch data access must run under current authenticated SQL login.
            if (_sqlLogin is null || _sqlPassword is null)
            {
                throw new InvalidOperationException(
                    "Chưa thiết lập thông tin đăng nhập SQL. Hãy gọi SetSqlLoginCredentials trước.");
            }

            return InjectCredentials(template, _sqlLogin, _sqlPassword);
        }

        public string? GetLookupConnection()
        {
            var template = _configuration["ConnectionStrings:LookupDatabase"];

            if (template is null)
            {
                return null;
            }

            // Logic: lookup queries must use the same SQL identity to keep permission model consistent.
            if (_sqlLogin is null || _sqlPassword is null)
            {
                throw new InvalidOperationException(
                    "Chưa thiết lập thông tin đăng nhập SQL. Hãy gọi SetSqlLoginCredentials trước.");
            }

            return InjectCredentials(template, _sqlLogin, _sqlPassword);
        }

        public IReadOnlyList<string> GetConfiguredBranchCodes()
        {
            var branchCodes = _configuration
                .GetSection("ConnectionStrings")
                .GetChildren()
                .Select(child => child.Key)
                .Where(key => key.StartsWith("Branch_", StringComparison.OrdinalIgnoreCase))
                .Select(key => key["Branch_".Length..].Trim().ToUpperInvariant())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return branchCodes;
        }

        public string DefaultBranch =>
            _configuration["DatabaseSettings:DefaultBranch"] ?? "BENTHANH";

        private string GetPublisherTemplate()
        {
            return _configuration["ConnectionStrings:Publisher"]
                ?? throw new InvalidOperationException(
                    "Không tìm thấy chuỗi kết nối Publisher trong cấu hình.");
        }

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
}
