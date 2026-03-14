using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Handles branch master-data persistence for create, update, and lookup operations.
    /// </summary>
    public class BranchRepository : IBranchRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly ILogger<BranchRepository> _logger;

        /// <summary>
        /// Initializes BranchRepository with required infrastructure dependencies.
        /// </summary>
        /// <param name="connectionStringProvider">Connection provider for resolving target SQL instances.</param>
        /// <param name="logger">Logger instance for repository diagnostics.</param>
        public BranchRepository(
            IConnectionStringProvider connectionStringProvider,
            ILogger<BranchRepository> logger)
        {
            _connectionStringProvider = connectionStringProvider;
            _logger = logger;
        }

        private SqlConnection CreateBankConnection() =>
            // Logic: branch master data is managed centrally on publisher.
            new SqlConnection(_connectionStringProvider.GetPublisherConnection());

        public async Task<List<Branch>> GetAllBranchesAsync()
        {
            var branches = new List<Branch>();
            try
            {
                using var connection = CreateBankConnection();
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_GetBranches", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    branches.Add(MapFromReader(reader));
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error retrieving branches: {ex.Message}", ex);
            }
            return branches;
        }

        public async Task<Branch?> GetBranchAsync(string macn)
        {
            try
            {
                using var connection = CreateBankConnection();
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_GetBranch", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@MACN", macn);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    return MapFromReader(reader);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error retrieving branch: {ex.Message}", ex);
            }
            return null;
        }

        public async Task<bool> AddBranchAsync(Branch branch)
        {
            try
            {
                using var connection = CreateBankConnection();
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_AddBranch", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@MACN", branch.MACN);
                command.Parameters.AddWithValue("@TENCN", branch.TENCN);
                command.Parameters.AddWithValue("@DIACHI", string.IsNullOrEmpty(branch.DiaChi) ? (object)DBNull.Value : branch.DiaChi);
                command.Parameters.AddWithValue("@SODT", string.IsNullOrEmpty(branch.SODT) ? (object)DBNull.Value : branch.SODT);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error adding branch: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateBranchAsync(Branch branch)
        {
            try
            {
                using var connection = CreateBankConnection();
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_UpdateBranch", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@MACN", branch.MACN);
                command.Parameters.AddWithValue("@TENCN", branch.TENCN);
                command.Parameters.AddWithValue("@DIACHI", string.IsNullOrEmpty(branch.DiaChi) ? (object)DBNull.Value : branch.DiaChi);
                command.Parameters.AddWithValue("@SODT", string.IsNullOrEmpty(branch.SODT) ? (object)DBNull.Value : branch.SODT);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error updating branch: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteBranchAsync(string macn)
        {
            try
            {
                using var connection = CreateBankConnection();
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_DeleteBranch", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@MACN", macn);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error deleting branch: {ex.Message}", ex);
            }
        }

        public async Task<bool> BranchExistsAsync(string macn)
        {
            var branch = await GetBranchAsync(macn);
            return branch != null;
        }

        private static Branch MapFromReader(SqlDataReader reader) => new()
        {
            MACN = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
            TENCN = reader.GetString(reader.GetOrdinal("TENCN")),
            DiaChi = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? string.Empty : reader.GetString(reader.GetOrdinal("DIACHI")),
            SODT = reader.IsDBNull(reader.GetOrdinal("SODT")) ? string.Empty : reader.GetString(reader.GetOrdinal("SODT"))
        };
    }
}
