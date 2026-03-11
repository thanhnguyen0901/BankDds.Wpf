using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// SQL Server implementation of IUserRepository using runtime auth/account SPs.
/// UI-first topology:
///   - Create account:      sp_TaoTaiKhoan
///   - Delete account:      sp_XoaTaiKhoan
///   - Reset password:      sp_DoiMatKhau
///   - List accounts/roles: sp_DanhSachNhanVien
/// Transitional CRUD SPs on NGUOIDUNG are intentionally not used here.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IConnectionStringProvider connectionStringProvider,
        ILogger<UserRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _logger = logger;
    }

    private string GetConnectionString() => _connectionStringProvider.GetPublisherConnection();

    public async Task<User?> GetUserAsync(string username)
    {
        var users = await GetAllUsersAsync();
        return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> AddUserAsync(User user)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_TaoTaiKhoan", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@LOGIN", user.Username);
            command.Parameters.AddWithValue("@PASS", user.PasswordHash);
            command.Parameters.AddWithValue("@TENNHOM", ToSqlRoleName(user.UserGroup));

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error creating login '{user.Username}': {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DoiMatKhau", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@LOGIN", user.Username);
            // Password reset path (NGANHANG role). Caller self-change with old password
            // is not modeled in this admin repository.
            command.Parameters.AddWithValue("@PASSCU", DBNull.Value);
            command.Parameters.AddWithValue("@PASSMOI", user.PasswordHash);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error resetting password for '{user.Username}': {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_XoaTaiKhoan", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@LOGIN", username);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error deleting login '{username}': {ex.Message}", ex);
        }
    }

    public async Task<bool> RestoreUserAsync(string username)
    {
        _logger.LogWarning("RestoreUserAsync is not supported in SQL-login mode. Username={Username}", username);
        return false;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_DanhSachNhanVien", connection) { CommandType = CommandType.StoredProcedure };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapFromRoleReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving logins: {ex.Message}", ex);
        }
        return users;
    }

    private static string ToSqlRoleName(UserGroup group)
    {
        return group switch
        {
            UserGroup.NganHang => "NGANHANG",
            UserGroup.ChiNhanh => "CHINHANH",
            UserGroup.KhachHang => "KHACHHANG",
            _ => throw new InvalidOperationException($"Unsupported user group: {group}")
        };
    }

    private static UserGroup FromSqlRoleName(string roleName)
    {
        return roleName.Trim().ToUpperInvariant() switch
        {
            "NGANHANG" => UserGroup.NganHang,
            "CHINHANH" => UserGroup.ChiNhanh,
            "KHACHHANG" => UserGroup.KhachHang,
            _ => throw new InvalidOperationException($"Unknown SQL role '{roleName}'.")
        };
    }

    private static User MapFromRoleReader(SqlDataReader reader)
    {
        var login = reader.GetString(reader.GetOrdinal("LOGINNAME"));
        var role = reader.GetString(reader.GetOrdinal("TENNHOM"));
        var userGroup = FromSqlRoleName(role);

        // In SQL-login mode we no longer keep soft-delete state in NGUOIDUNG.
        // Deleted logins simply disappear from the listing.
        var model = new User
        {
            Username = login,
            UserGroup = userGroup,
            DefaultBranch = string.Empty,
            TrangThaiXoa = 0
        };

        // Heuristic display fields to keep existing grid columns meaningful.
        if (userGroup == UserGroup.KhachHang)
            model.CustomerCMND = login;
        else
            model.EmployeeId = login;

        return model;
    }
}

