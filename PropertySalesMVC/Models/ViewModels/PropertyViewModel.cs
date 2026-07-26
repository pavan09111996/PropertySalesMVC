namespace PropertySalesMVC.Models
{
    public class PropertyViewModel
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int BHK { get; set; }
        public int? LocationId { get; set; }
        public int LookingFor { get; set; }
        public bool IsFeatured { get; set; }

        public List<string> Images { get; set; } = new();
        public string ImagePath { get; set; }
        public List<string> ImagePaths { get; set; } = new();
        public string VideoPath { get; set; }
    }
}
