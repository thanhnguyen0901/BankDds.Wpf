using BankDds.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace BankDds.Infrastructure.Security;

/// <summary>
/// Banking authentication: connects to Publisher with the entered SQL login/password
/// and calls sp_DangNhap to resolve role (NGANHANG / CHINHANH / KHACHHANG).
/// No BCrypt, no NGUOIDUNG table — SQL Server handles credential verification.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public AuthService(IConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
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

            // sp_DangNhap returns: MANV (SYSTEM_USER), HOTEN (USER_NAME()), TENNHOM
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

            // Store the SQL login credentials in the provider so all subsequent
            // DB calls (repositories, services) use the same identity.
            _connectionStringProvider.SetSqlLoginCredentials(userName, password);

            return new AuthResult
            {
                Success = true,
                UserGroup = userGroup,
                EmployeeId = manv,
                DisplayName = hoTen
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
