namespace PropertySalesMVC.Models
{
    public class AdminPropertyFilter
    {
        public string? Search { get; set; }
        public int? LookingFor { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
