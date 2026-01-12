using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel.DataAnnotations;

namespace PropertySalesMVC.Models
{
    public class Property
    {
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Location { get; set; }

        public decimal Price { get; set; }

        public string? Description { get; set; }

        // Stores image file names (comma separated)
        public string? ImagePaths { get; set; }
    }

    public class PropertyViewModel
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }

        public List<string> ImagesBase64 { get; set; } = new();
    }

    public class PropertySummary
    {
        public int TotalProperties { get; set; }
    }

}
