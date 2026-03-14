using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// SQL Server implementation of IUserRepository using runtime auth/account SPs
/// plus NGUOIDUNG mapping persistence.
/// UI-first topology:
///   - Create account:      sp_TaoTaiKhoan
///   - Create app mapping:  USP_AddUser
///   - Delete account:      sp_XoaTaiKhoan
///   - Reset password:      sp_DoiMatKhau
///   - List mappings:       SP_GetAllUsers
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

            // 1) Create SQL login + db user + role membership.
            using (var createLoginCommand = new SqlCommand("sp_TaoTaiKhoan", connection) { CommandType = CommandType.StoredProcedure })
            {
                createLoginCommand.Parameters.AddWithValue("@LOGIN", user.Username);
                createLoginCommand.Parameters.AddWithValue("@PASS", user.PasswordHash);
                createLoginCommand.Parameters.AddWithValue("@TENNHOM", ToSqlRoleName(user.UserGroup));
                await createLoginCommand.ExecuteNonQueryAsync();
            }

            // 2) Persist NGUOIDUNG mapping used by app session resolution.
            using (var addMappingCommand = new SqlCommand("USP_AddUser", connection) { CommandType = CommandType.StoredProcedure })
            {
                addMappingCommand.Parameters.AddWithValue("@Username", user.Username);
                addMappingCommand.Parameters.AddWithValue("@PasswordHash", "N/A-SQL-AUTH");
                addMappingCommand.Parameters.AddWithValue("@UserGroup", (int)user.UserGroup);
                addMappingCommand.Parameters.AddWithValue("@DefaultBranch", NormalizeBranchCode(user.DefaultBranch));
                addMappingCommand.Parameters.AddWithValue("@CustomerCMND", ToDbValueOrNull(user.CustomerCMND));
                addMappingCommand.Parameters.AddWithValue("@EmployeeId", ToDbValueOrNull(user.EmployeeId));
                await addMappingCommand.ExecuteNonQueryAsync();
            }

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

            using (var deleteLoginCommand = new SqlCommand("sp_XoaTaiKhoan", connection) { CommandType = CommandType.StoredProcedure })
            {
                deleteLoginCommand.Parameters.AddWithValue("@LOGIN", username);
                await deleteLoginCommand.ExecuteNonQueryAsync();
            }

            // Keep NGUOIDUNG state consistent with SQL login lifecycle.
            using (var softDeleteMappingCommand = new SqlCommand("SP_SoftDeleteUser", connection) { CommandType = CommandType.StoredProcedure })
            {
                softDeleteMappingCommand.Parameters.AddWithValue("@Username", username);
                await softDeleteMappingCommand.ExecuteNonQueryAsync();
            }

            return true;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error deleting login '{username}': {ex.Message}", ex);
        }
    }

    public async Task<bool> RestoreUserAsync(string username)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_RestoreUser", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Username", username);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "RestoreUserAsync failed for Username={Username}", username);
            return false;
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetAllUsers", connection) { CommandType = CommandType.StoredProcedure };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapFromNguoiDungReader(reader));
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

    private static object ToDbValueOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }

    private static string NormalizeBranchCode(string branchCode)
    {
        return branchCode.Trim().ToUpperInvariant();
    }

    private static UserGroup FromSqlRoleCode(int userGroupCode)
    {
        return userGroupCode switch
        {
            0 => UserGroup.NganHang,
            1 => UserGroup.ChiNhanh,
            2 => UserGroup.KhachHang,
            _ => throw new InvalidOperationException($"Unknown UserGroup code '{userGroupCode}'.")
        };
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetString(ordinal).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static User MapFromNguoiDungReader(SqlDataReader reader)
    {
        return new User
        {
            Username = reader.GetString(reader.GetOrdinal("Username")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            UserGroup = FromSqlRoleCode(reader.GetInt32(reader.GetOrdinal("UserGroup"))),
            DefaultBranch = reader.GetString(reader.GetOrdinal("DefaultBranch")).Trim().ToUpperInvariant(),
            CustomerCMND = ReadNullableString(reader, "CustomerCMND"),
            EmployeeId = ReadNullableString(reader, "EmployeeId"),
            TrangThaiXoa = reader.GetByte(reader.GetOrdinal("TrangThaiXoa"))
        };
    }
}

