using System.ComponentModel.DataAnnotations;

namespace PropertySalesMVC.Models
{
    public class EditPropertyViewModel
    {
        /* =========================
           PROPERTY CORE FIELDS
        ========================= */

        public int PropertyId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int LookingFor { get; set; } // 1 = Rent, 2 = Buy, 3 = Sell

        [Required]
        public int BHK { get; set; }

        public bool IsFeatured { get; set; }

        /* =========================
           EXISTING IMAGES (DB)
        ========================= */

        public List<PropertyImageViewModel> ExistingImages { get; set; }
            = new List<PropertyImageViewModel>();

        /* =========================
           REMOVE EXISTING IMAGES
        ========================= */

        public List<int> RemoveImageIds { get; set; }
            = new List<int>();

        /* =========================
           NEW IMAGES (UPLOAD)
        ========================= */

        public List<IFormFile> NewImages { get; set; }
            = new List<IFormFile>();

        /* =========================
           VIDEO SUPPORT (SINGLE)
        ========================= */

        // Existing video path (from DB)
        public string? ExistingVideoPath { get; set; }

        // Upload new video
        public IFormFile? NewVideo { get; set; }

        // Checkbox to remove existing video
        public bool RemoveVideo { get; set; }
    }
}
