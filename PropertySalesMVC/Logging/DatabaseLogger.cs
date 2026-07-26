using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace PropertySalesMVC.Logging
{
    // Writes Error/Critical log entries to the ErrorLogs table. Deliberately narrow:
    // - Only Error/Critical (see IsEnabled) — this is not a general-purpose request logger.
    // - Fire-and-forget, swallows its own failures — logging must never crash the app
    //   it's trying to diagnose, especially if the failure IS the DB being unavailable.
    // - Self-purges rows older than 30 days on every write, so a noisy recurring error
    //   can never grow this table unbounded against the DB's tight storage budget.
    public class DatabaseLogger : ILogger
    {
        private const int MaxMessageLength = 1000;
        private const int MaxStackTraceLength = 4000;
        private const int MaxExceptionTypeLength = 300;
        private const int MaxSourceLength = 300;
        private const int MaxRequestPathLength = 500;

        private readonly string _categoryName;
        private readonly string _connectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DatabaseLogger(string categoryName, string connectionString, IHttpContextAccessor httpContextAccessor)
        {
            _categoryName = categoryName;
            _connectionString = connectionString;
            _httpContextAccessor = httpContextAccessor;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string severity = logLevel == LogLevel.Critical ? "Critical" : "Error";
            string message = Truncate(formatter(state, exception), MaxMessageLength);
            string? exceptionType = exception?.GetType().FullName is { } t ? Truncate(t, MaxExceptionTypeLength) : null;
            string? stackTrace = exception?.StackTrace is { } s ? Truncate(s, MaxStackTraceLength) : null;
            string source = Truncate(_categoryName, MaxSourceLength);

            string? requestPath = null;
            try
            {
                requestPath = _httpContextAccessor.HttpContext?.Request?.Path.ToString();
            }
            catch
            {
                // HttpContext can be unavailable (background work, startup logging) — fine to skip.
            }
            if (requestPath != null)
                requestPath = Truncate(requestPath, MaxRequestPathLength);

            // Fire-and-forget on purpose: logging must not add latency to (or block) the
            // request that triggered it.
            _ = WriteAsync(severity, message, exceptionType, stackTrace, source, requestPath);
        }

        private async Task WriteAsync(
            string severity, string message, string? exceptionType, string? stackTrace, string source, string? requestPath)
        {
            try
            {
                await using var con = new SqlConnection(_connectionString);
                await con.OpenAsync();

                await using (var insertCmd = new SqlCommand(@"
                    INSERT INTO ErrorLogs (Severity, Message, ExceptionType, StackTrace, Source, RequestPath, CreatedOn)
                    VALUES (@Severity, @Message, @ExceptionType, @StackTrace, @Source, @RequestPath, GETDATE())", con))
                {
                    insertCmd.Parameters.AddWithValue("@Severity", severity);
                    insertCmd.Parameters.AddWithValue("@Message", message);
                    insertCmd.Parameters.AddWithValue("@ExceptionType", (object?)exceptionType ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@StackTrace", (object?)stackTrace ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Source", source);
                    insertCmd.Parameters.AddWithValue("@RequestPath", (object?)requestPath ?? DBNull.Value);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                await using (var purgeCmd = new SqlCommand(
                    "DELETE FROM ErrorLogs WHERE CreatedOn < DATEADD(day, -30, GETDATE())", con))
                {
                    await purgeCmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Nowhere safe left to report this — deliberately swallowed.
            }
        }

        private static string Truncate(string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength];
    }
}
