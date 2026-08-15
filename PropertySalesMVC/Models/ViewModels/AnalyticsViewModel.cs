namespace PropertySalesMVC.Models
{
    // Aggregate view counts for one location, split by mode — feeds the
    // "top locations" table on the admin Analytics page.
    public class LocationViewStat
    {
        public string LocationName { get; set; } = "All Locations";
        public int BuyViews { get; set; }
        public int RentViews { get; set; }
        public int TotalViews { get; set; }
    }

    // Aggregate view counts for one BHK size, split by mode — feeds the
    // "top BHK" table on the admin Analytics page.
    public class BhkViewStat
    {
        public int? BHK { get; set; }
        public string BhkLabel => BHK.HasValue ? $"{BHK} BHK" : "Any BHK";
        public int BuyViews { get; set; }
        public int RentViews { get; set; }
        public int TotalViews { get; set; }
    }

    // One point on the admin Analytics page's daily-trend sparkline/table.
    public class DailyViewStat
    {
        public DateTime ViewDate { get; set; }
        public int TotalViews { get; set; }
    }

    // Raw cell from the DB — one (location, BHK, mode) combination and its count.
    // Shaped into LocationBhkHeatmap by AnalyticsService before reaching the view.
    public class LocationBhkCell
    {
        public string LocationName { get; set; } = "All Locations";
        public int? BHK { get; set; }
        public string Mode { get; set; } = "";
        public int ViewCount { get; set; }
    }

    // One row of the Location x BHK heatmap grid — a location, with its view
    // count for every BHK column, split by mode.
    public class HeatmapRow
    {
        public string LocationName { get; set; } = "";
        public List<int> BuyValues { get; set; } = new();
        public List<int> RentValues { get; set; } = new();
    }

    // The full Location x BHK intersection — answers "Kandivali + 2BHK + Rent"
    // directly, unlike TopLocations/TopBhk which are each marginalized over the
    // other dimension. Rows cover every active location (even ones with zero
    // views in range); columns are the fixed BHK sizes the app supports (1-4)
    // plus "Any BHK" (no BHK filter applied).
    public class LocationBhkHeatmap
    {
        public List<string> BhkColumns { get; set; } = new();
        public List<HeatmapRow> Rows { get; set; } = new();
        public int MaxBuyValue { get; set; }
        public int MaxRentValue { get; set; }
    }

    public class AnalyticsViewModel
    {
        public int DaysRange { get; set; }
        public int TotalViews { get; set; }
        public int BuyViews { get; set; }
        public int RentViews { get; set; }
        public List<LocationViewStat> TopLocations { get; set; } = new();
        public List<BhkViewStat> TopBhk { get; set; } = new();
        public List<DailyViewStat> DailyTrend { get; set; } = new();
        public LocationBhkHeatmap Heatmap { get; set; } = new();
    }
}
