using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Handles cross-branch customer lookup queries used by bank-level search features.
    /// </summary>
    public class CustomerLookupRepository : ICustomerLookupRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly ILogger<CustomerLookupRepository> _logger;

        /// <summary>
        /// Initializes CustomerLookupRepository with required infrastructure dependencies.
        /// </summary>
        /// <param name="connectionStringProvider">Connection provider for resolving target SQL instances.</param>
        /// <param name="logger">Logger instance for repository diagnostics.</param>
        public CustomerLookupRepository(
            IConnectionStringProvider connectionStringProvider,
            ILogger<CustomerLookupRepository> logger)
        {
            _connectionStringProvider = connectionStringProvider;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            // Logic: prefer dedicated lookup replica; fallback to publisher when lookup connection is not configured.
            return _connectionStringProvider.GetLookupConnection()
                ?? _connectionStringProvider.GetPublisherConnection();
        }

        public async Task<Customer?> GetCustomerByCmndAsync(string cmnd)
        {
            cmnd = cmnd.Trim();
            _logger.LogInformation("CustomerLookup: lookup CMND={Cmnd}", cmnd);
            try
            {
                await using var connection = new SqlConnection(GetConnectionString());
                await connection.OpenAsync();
                await using var cmd = new SqlCommand("dbo.SP_GetCustomerByCMND", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.Add("@CMND", SqlDbType.NChar, 10).Value = cmnd;
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    return MapFromReader(reader);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"CustomerLookup: database error looking up CMND '{cmnd}': {ex.Message}", ex);
            }
            return null;
        }

        public async Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50)
        {
            keyword = keyword.Trim();
            _logger.LogInformation("CustomerLookup: search name keyword={Keyword}, max={Max}", keyword, maxResults);
            try
            {
                var results = new List<Customer>();
                await using var connection = new SqlConnection(GetConnectionString());
                await connection.OpenAsync();
                await using var cmd = new SqlCommand("dbo.SP_SearchCustomersByName", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.Add("@MaxRows", SqlDbType.Int).Value = maxResults;
                cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = keyword;
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    results.Add(MapFromReader(reader));
                return results;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"CustomerLookup: database error searching name '{keyword}': {ex.Message}", ex);
            }
        }

        private static Customer MapFromReader(SqlDataReader reader) => new()
        {
            CMND = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
            Ho = reader.GetString(reader.GetOrdinal("HO")),
            Ten = reader.GetString(reader.GetOrdinal("TEN")),
            NgaySinh = reader.IsDBNull(reader.GetOrdinal("NGAYSINH")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYSINH")),
            DiaChi = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? "" : reader.GetString(reader.GetOrdinal("DIACHI")),
            NgayCap = reader.IsDBNull(reader.GetOrdinal("NGAYCAP")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYCAP")),
            SODT = reader.IsDBNull(reader.GetOrdinal("SODT")) ? "" : reader.GetString(reader.GetOrdinal("SODT")),
            Phai = reader.GetString(reader.GetOrdinal("PHAI")).Trim(),
            MaCN = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
            TrangThaiXoa = reader.IsDBNull(reader.GetOrdinal("TrangThaiXoa")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("TrangThaiXoa")))
        };
    }
}
