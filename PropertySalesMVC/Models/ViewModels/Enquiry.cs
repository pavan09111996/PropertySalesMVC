namespace PropertySalesMVC.Models
{
    public class Enquiry
    {
        public int Id { get; set; }
        public string EnquiryType { get; set; } = "";
        public int? PropertyId { get; set; }
        public string? PropertyTitle { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Message { get; set; }
        public string? Title { get; set; }
        public int? BHK { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class NewEnquiry
    {
        public string EnquiryType { get; set; } = "";
        public int? PropertyId { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Message { get; set; }
        public string? Title { get; set; }
        public int? BHK { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
    }
}
