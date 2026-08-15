using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public interface IAnalyticsService
    {
        Task RecordListingViewAsync(string mode, int? locationId, int? bhk);
        Task<AnalyticsViewModel> GetAnalyticsAsync(int days);
    }
}
