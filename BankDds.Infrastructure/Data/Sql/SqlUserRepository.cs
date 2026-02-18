using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of IUserRepository using ADO.NET.
/// Users are stored in the main bank database (not per-branch).
/// All SqlExceptions are wrapped in InvalidOperationException with a user-friendly message.
/// </summary>
public class SqlUserRepository : IUserRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly ILogger<SqlUserRepository> _logger;

    public SqlUserRepository(
        IConnectionStringProvider connectionStringProvider,
        ILogger<SqlUserRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _logger = logger;
    }

    private string GetConnectionString() => _connectionStringProvider.GetBankConnection();

    public async Task<User?> GetUserAsync(string username)
    {
        // Logging: record attempt with username only — password is NEVER logged.
        _logger.LogInformation("Auth lookup: user={Username}", username);
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetUser", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Username", username);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return MapFromReader(reader);
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving user '{username}': {ex.Message}", ex); }
        return null;
    }

    public async Task<bool> AddUserAsync(User user)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_AddUser", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Username",      user.Username);
            command.Parameters.AddWithValue("@PasswordHash",  user.PasswordHash);
            command.Parameters.AddWithValue("@UserGroup",     (int)user.UserGroup);
            command.Parameters.AddWithValue("@DefaultBranch", user.DefaultBranch);
            command.Parameters.AddWithValue("@CustomerCMND",  (object?)user.CustomerCMND ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmployeeId",    (object?)user.EmployeeId   ?? DBNull.Value);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error adding user: {ex.Message}", ex); }
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_UpdateUser", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Username",      user.Username);
            command.Parameters.AddWithValue("@PasswordHash",  user.PasswordHash);
            command.Parameters.AddWithValue("@UserGroup",     (int)user.UserGroup);
            command.Parameters.AddWithValue("@DefaultBranch", user.DefaultBranch);
            command.Parameters.AddWithValue("@CustomerCMND",  (object?)user.CustomerCMND ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmployeeId",    (object?)user.EmployeeId   ?? DBNull.Value);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error updating user: {ex.Message}", ex); }
    }

    /// <summary>
    /// Soft-deletes a user by setting TRANGTHAIXED = 1.
    /// SP contract: SP_SoftDeleteUser @Username nvarchar(50)
    ///   UPDATE NGUOIDUNG SET TRANGTHAIXED = 1 WHERE Username = @Username
    /// </summary>
    public async Task<bool> DeleteUserAsync(string username)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_SoftDeleteUser", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Username", username);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error soft-deleting user '{username}': {ex.Message}", ex); }
    }

    /// <summary>
    /// Restores a soft-deleted user by setting TRANGTHAIXED = 0.
    /// SP contract: SP_RestoreUser @Username nvarchar(50)
    ///   UPDATE NGUOIDUNG SET TRANGTHAIXED = 0 WHERE Username = @Username
    /// </summary>
    public async Task<bool> RestoreUserAsync(string username)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_RestoreUser", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Username", username);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error restoring user '{username}': {ex.Message}", ex); }
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
            while (await reader.ReadAsync()) users.Add(MapFromReader(reader));
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving users: {ex.Message}", ex); }
        return users;
    }

    private static User MapFromReader(SqlDataReader reader)
    {
        var trangThaiOrdinal = reader.GetOrdinal("TrangThaiXoa");
        return new User
        {
            // nChar columns are space-padded — Trim() normalises for model comparisons.
            Username      = reader.GetString(reader.GetOrdinal("Username")),         // nvarchar — no Trim needed
            PasswordHash  = reader.GetString(reader.GetOrdinal("PasswordHash")),     // nvarchar
            UserGroup     = (UserGroup)reader.GetInt32(reader.GetOrdinal("UserGroup")),
            DefaultBranch = reader.GetString(reader.GetOrdinal("DefaultBranch")).Trim(),
            CustomerCMND  = reader.IsDBNull(reader.GetOrdinal("CustomerCMND")) ? null : reader.GetString(reader.GetOrdinal("CustomerCMND")).Trim(),
            EmployeeId    = reader.IsDBNull(reader.GetOrdinal("EmployeeId"))   ? null : reader.GetString(reader.GetOrdinal("EmployeeId")).Trim(),
            TrangThaiXoa  = reader.IsDBNull(trangThaiOrdinal) ? 0 : reader.GetByte(trangThaiOrdinal)
        };
    }
}
