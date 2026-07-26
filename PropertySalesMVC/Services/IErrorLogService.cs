using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public interface IErrorLogService
    {
        Task<(List<ErrorLogEntry> Entries, int TotalCount)> GetErrorLogsAsync(int page, int pageSize);
    }
}
