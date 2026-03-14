using BankDds.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BankDds.Infrastructure.Security
{
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
                var connectionString =
                    _connectionStringProvider.GetPublisherConnectionForLogin(userName, password);
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
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
                        ErrorMessage = "sp_DangNhap không trả về dữ liệu."
                    };
                }
                var manv = ReadOptionalString(reader, "MANV");
                var hoTen = ReadOptionalString(reader, "HOTEN");
                var tenNhom = ReadOptionalString(reader, "TENNHOM");
                var defaultBranch = ReadOptionalString(reader, "MACN");
                var customerCmnd = ReadOptionalString(reader, "CustomerCMND");
                var employeeId = ReadOptionalString(reader, "EmployeeId");
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
                        ErrorMessage = $"Vai trò '{tenNhom}' trả về từ sp_DangNhap không hợp lệ."
                    };
                }
                if (string.IsNullOrWhiteSpace(hoTen))
                    hoTen = string.IsNullOrWhiteSpace(manv) ? userName : manv;
                if (userGroup is "ChiNhanh" or "KhachHang")
                {
                    if (string.IsNullOrWhiteSpace(defaultBranch))
                    {
                        _logger.LogWarning(
                            "Login '{Login}' resolved role {Role} but no MACN from sp_DangNhap.",
                            userName, userGroup);
                        return new AuthResult
                        {
                            Success = false,
                            ErrorMessage = "Tài khoản đăng nhập chưa được gán chi nhánh (MACN)."
                        };
                    }
                    defaultBranch = defaultBranch.Trim().ToUpperInvariant();
                }
                if (userGroup == "ChiNhanh")
                {
                    employeeId = string.IsNullOrWhiteSpace(employeeId) ? manv : employeeId;
                    if (string.IsNullOrWhiteSpace(employeeId))
                    {
                        _logger.LogWarning(
                            "Login '{Login}' resolved CHINHANH but no EmployeeId/MANV mapping.",
                            userName);
                        return new AuthResult
                        {
                            Success = false,
                            ErrorMessage = "Tài khoản ChiNhanh chưa được gán EmployeeId."
                        };
                    }
                }
                if (userGroup == "KhachHang")
                {
                    if (string.IsNullOrWhiteSpace(customerCmnd))
                    {
                        _logger.LogWarning(
                            "Login '{Login}' resolved KHACHHANG but no CustomerCMND mapping.",
                            userName);
                        return new AuthResult
                        {
                            Success = false,
                            ErrorMessage = "Tài khoản KhachHang chưa được gán CustomerCMND."
                        };
                    }
                }
                _connectionStringProvider.SetSqlLoginCredentials(userName, password);
                return new AuthResult
                {
                    Success = true,
                    UserGroup = userGroup,
                    EmployeeId = employeeId,
                    DisplayName = hoTen,
                    DefaultBranch = defaultBranch,
                    CustomerCMND = customerCmnd
                };
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "SQL login failed for account '{Login}'.", userName);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Đăng nhập thất bại: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for account '{Login}'.", userName);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Lỗi: {ex.Message}"
                };
            }
        }

        public Task LogoutAsync()
        {
            _connectionStringProvider.ClearSqlLoginCredentials();
            return Task.CompletedTask;
        }

        private static string ReadOptionalString(SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return string.Empty;
                return reader.GetValue(ordinal)?.ToString()?.Trim() ?? string.Empty;
            }
            catch (IndexOutOfRangeException)
            {
                return string.Empty;
            }
        }
    }
}