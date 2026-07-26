namespace PropertySalesMVC.Models
{
    public class PropertyFilterVM
    {
        public string Mode { get; set; }      // Buy / Rent / Sell
        public int? LocationId { get; set; }  // nullable
        public int? BHK { get; set; }         // nullable

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }
}
