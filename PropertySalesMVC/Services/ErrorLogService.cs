using PropertySalesMVC.Models;
using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public ErrorLogService(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        public async Task<(List<ErrorLogEntry> Entries, int TotalCount)> GetErrorLogsAsync(int page, int pageSize)
        {
            var entries = await _errorLogRepository.GetErrorLogsAsync(page, pageSize);
            var total = await _errorLogRepository.GetErrorLogCountAsync();
            return (entries, total);
        }
    }
}
