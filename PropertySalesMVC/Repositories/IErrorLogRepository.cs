using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public interface IErrorLogRepository
    {
        Task<List<ErrorLogEntry>> GetErrorLogsAsync(int page, int pageSize);
        Task<int> GetErrorLogCountAsync();
    }
}
