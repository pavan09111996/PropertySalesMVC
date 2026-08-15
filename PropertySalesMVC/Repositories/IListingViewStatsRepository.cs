using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public interface IListingViewStatsRepository
    {
        Task RecordViewAsync(string mode, int? locationId, int? bhk);
        Task<List<LocationViewStat>> GetTopLocationsAsync(DateTime since);
        Task<List<BhkViewStat>> GetTopBhkAsync(DateTime since);
        Task<List<DailyViewStat>> GetDailyTrendAsync(DateTime since);
        Task<(int TotalViews, int BuyViews, int RentViews)> GetTotalsAsync(DateTime since);
        Task<List<LocationBhkCell>> GetLocationBhkBreakdownAsync(DateTime since);
    }
}
