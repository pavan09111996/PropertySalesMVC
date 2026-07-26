using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PropertySalesMVC.Logging
{
    [ProviderAlias("Database")]
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly string _connectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<string, DatabaseLogger> _loggers = new();

        public DatabaseLoggerProvider(string connectionString, IHttpContextAccessor httpContextAccessor)
        {
            _connectionString = connectionString;
            _httpContextAccessor = httpContextAccessor;
        }

        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, name => new DatabaseLogger(name, _connectionString, _httpContextAccessor));

        public void Dispose() => _loggers.Clear();
    }
}
