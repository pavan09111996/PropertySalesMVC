using PropertySalesMVC.Models;
using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IListingViewStatsRepository _repository;
        private readonly ILocationRepository _locationRepository;

        public AnalyticsService(IListingViewStatsRepository repository, ILocationRepository locationRepository)
        {
            _repository = repository;
            _locationRepository = locationRepository;
        }

        public Task RecordListingViewAsync(string mode, int? locationId, int? bhk)
        {
            // Only Buy/Rent are browsable by location+BHK — Sell has no listing
            // grid (it's a submission form), so there's nothing meaningful to count.
            if (mode != "Buy" && mode != "Rent")
                return Task.CompletedTask;

            return _repository.RecordViewAsync(mode, locationId, bhk);
        }

        public async Task<AnalyticsViewModel> GetAnalyticsAsync(int days)
        {
            var since = DateTime.Today.AddDays(-(days - 1));

            var totals = await _repository.GetTotalsAsync(since);
            var cells = await _repository.GetLocationBhkBreakdownAsync(since);
            var locations = await _locationRepository.GetActiveLocationsAsync();

            return new AnalyticsViewModel
            {
                DaysRange = days,
                TotalViews = totals.TotalViews,
                BuyViews = totals.BuyViews,
                RentViews = totals.RentViews,
                TopLocations = await _repository.GetTopLocationsAsync(since),
                TopBhk = await _repository.GetTopBhkAsync(since),
                DailyTrend = await _repository.GetDailyTrendAsync(since),
                Heatmap = BuildHeatmap(cells, locations)
            };
        }

        // Shapes the raw (location, BHK, mode) cells into a fixed grid: every
        // active location gets a row (even with zero views), "All Locations" is
        // appended for the no-filter bucket, and columns are the app's fixed
        // BHK range (1-4) plus "Any BHK" — so the grid's shape never changes
        // even as the underlying data grows.
        private static LocationBhkHeatmap BuildHeatmap(List<LocationBhkCell> cells, List<LocationOption> locations)
        {
            var rowNames = locations.Select(l => l.Location).ToList();
            rowNames.Add("All Locations");

            var bhkValues = new int?[] { 1, 2, 3, 4, null };
            var bhkLabels = bhkValues.Select(b => b.HasValue ? $"{b} BHK" : "Any BHK").ToList();

            var lookup = cells.ToDictionary(c => (c.LocationName, c.BHK, c.Mode), c => c.ViewCount);

            var heatmap = new LocationBhkHeatmap { BhkColumns = bhkLabels };
            int maxBuy = 0, maxRent = 0;

            foreach (var locationName in rowNames)
            {
                var row = new HeatmapRow { LocationName = locationName };

                foreach (var bhk in bhkValues)
                {
                    int buyVal = lookup.GetValueOrDefault((locationName, bhk, "Buy"), 0);
                    int rentVal = lookup.GetValueOrDefault((locationName, bhk, "Rent"), 0);

                    row.BuyValues.Add(buyVal);
                    row.RentValues.Add(rentVal);

                    maxBuy = Math.Max(maxBuy, buyVal);
                    maxRent = Math.Max(maxRent, rentVal);
                }

                heatmap.Rows.Add(row);
            }

            heatmap.MaxBuyValue = maxBuy;
            heatmap.MaxRentValue = maxRent;
            return heatmap;
        }
    }
}
