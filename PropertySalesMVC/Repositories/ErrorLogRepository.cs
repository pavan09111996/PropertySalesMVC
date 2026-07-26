using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    // Read-only by design — writes go through Logging/DatabaseLogger.cs (an ILogger
    // implementation, outside the normal DI-scoped repository lifetime), not this class.
    public class ErrorLogRepository : IErrorLogRepository
    {
        private readonly string _connectionString;

        public ErrorLogRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        private SqlConnection CreateConnection() => new(_connectionString);

        public async Task<List<ErrorLogEntry>> GetErrorLogsAsync(int page, int pageSize)
        {
            var entries = new List<ErrorLogEntry>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT Id, Severity, Message, ExceptionType, StackTrace, Source, RequestPath, CreatedOn
                FROM ErrorLogs
                ORDER BY CreatedOn DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(new ErrorLogEntry
                {
                    Id = reader.GetInt32(0),
                    Severity = reader.GetString(1),
                    Message = reader.GetString(2),
                    ExceptionType = reader.IsDBNull(3) ? null : reader.GetString(3),
                    StackTrace = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Source = reader.IsDBNull(5) ? null : reader.GetString(5),
                    RequestPath = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedOn = reader.GetDateTime(7)
                });
            }

            return entries;
        }

        public async Task<int> GetErrorLogCountAsync()
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand("SELECT COUNT(*) FROM ErrorLogs", con);

            await con.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
        }
    }
}
