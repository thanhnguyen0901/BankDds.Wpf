using BankDds.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BankDds.Infrastructure.Security;

/// <summary>
/// Banking authentication: connects to Publisher with the entered SQL login/password
/// and calls sp_DangNhap to resolve role (NGANHANG / CHINHANH / KHACHHANG).
/// No BCrypt, no NGUOIDUNG table — SQL Server handles credential verification.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IConnectionStringProvider connectionStringProvider,
        ILogger<AuthService> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string userName, string password)
    {
        try
        {
            // Build a Publisher connection string using the entered SQL login/password.
            // If credentials are wrong, SqlConnection.OpenAsync throws SqlException.
            var connectionString =
                _connectionStringProvider.GetPublisherConnectionForLogin(userName, password);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // sp_DangNhap returns: MANV, HOTEN, TENNHOM, and (after schema update) MACN.
            await using var cmd = new SqlCommand("sp_DangNhap", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "sp_DangNhap returned no rows."
                };
            }

            var manv = reader["MANV"]?.ToString() ?? string.Empty;
            var hoTen = reader["HOTEN"]?.ToString() ?? manv;
            var tenNhom = reader["TENNHOM"]?.ToString() ?? string.Empty;

            // Read MACN column if present (added by updated sp_DangNhap).
            // Older SP versions return only 3 columns — handle gracefully.
            string defaultBranch = string.Empty;
            try
            {
                int macnOrdinal = reader.GetOrdinal("MACN");
                if (!reader.IsDBNull(macnOrdinal))
                    defaultBranch = reader.GetString(macnOrdinal).Trim();
            }
            catch (IndexOutOfRangeException)
            {
                // MACN column not present — old SP version; will use C# fallback.
                _logger.LogWarning(
                    "sp_DangNhap did not return MACN column for login '{Login}'. " +
                    "Consider re-running sql/04_publisher_security.sql to update the SP.",
                    userName);
            }

            // Map SQL role name to the UserGroup string used by the WPF layer
            string userGroup = tenNhom.ToUpperInvariant() switch
            {
                "NGANHANG" => "NganHang",
                "CHINHANH" => "ChiNhanh",
                "KHACHHANG" => "KhachHang",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(userGroup))
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Unknown role '{tenNhom}' returned by sp_DangNhap."
                };
            }

            // Log a warning when branch-dependent roles have no DefaultBranch from SP.
            // LoginViewModel will apply a C#-side fallback using the login form selection.
            if (string.IsNullOrEmpty(defaultBranch) && userGroup is "ChiNhanh" or "KhachHang")
            {
                _logger.LogWarning(
                    "sp_DangNhap returned empty MACN for '{Login}' (role={Role}). " +
                    "Branch will be inferred from the login form selection.",
                    userName, userGroup);
            }

            // Store the SQL login credentials in the provider so all subsequent
            // DB calls (repositories, services) use the same identity.
            _connectionStringProvider.SetSqlLoginCredentials(userName, password);

            return new AuthResult
            {
                Success = true,
                UserGroup = userGroup,
                EmployeeId = manv,
                DisplayName = hoTen,
                DefaultBranch = defaultBranch
            };
        }
        catch (SqlException ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = $"Login failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = $"Error: {ex.Message}"
            };
        }
    }

    public Task LogoutAsync()
    {
        _connectionStringProvider.ClearSqlLoginCredentials();
        return Task.CompletedTask;
    }
}
