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
}
