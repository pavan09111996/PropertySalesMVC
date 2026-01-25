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
        public int BHK { get; set; }
        public int? LocationId { get; set; }

        public List<string> ImagesBase64 { get; set; } = new();
        public List<string> Images { get; set; } = new();
        public string ImagePath { get; set; }
        public List<string> ImagePaths { get; set; } = new();
        public string VideoPath { get; set; }

    }

    public class PropertySummary
    {
        public int TotalProperties { get; set; }
    }

    public class LocationViewModel
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public string? LocationName { get; internal set; }
    }
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

    public class PropertyImageViewModel
    {
      public int ImageId { get; set; }

    // Preferred (file-based)
    public string? ImagePath { get; set; }

    // Legacy fallback (Base64)
    public string? ImageBase64 { get; set; }

    }

    public class AdminDetails
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string OwnerName { get; set; }
        public string Designation { get; set; }

        public string HeadOfficeTitle { get; set; }
        public string HeadOfficeAddress { get; set; }

        public string BranchOfficeTitle { get; set; }
        public string BranchOfficeAddress { get; set; }

        public string InstagramUrl { get; set; }
        public string FacebookUrl { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int AdminId { get; set; }
    }

    public class LocationMaster
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
    }

    public class LocationMasterNew
    {
        public string Id { get; set; }
        public string Location { get; set; }
    }
    public enum PropertyMode
{
    Buy = 1,
    Rent = 2,
    Sell = 3
}


public class PropertyFilterVM
{
    public string Mode { get; set; }      // Buy / Rent
    public int? LocationId { get; set; }  // nullable
    public int? BHK { get; set; }          // nullable
    // 🔥 Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}

public class AddPropertyViewModel
{
    [Required]
    public string Title { get; set; }

    [Required]
    public int Location { get; set; }

    [Required]
    public decimal Price { get; set; }


    public string Description { get; set; }

    [Required]
    public int LookingFor { get; set; } // 1 = Rent, 2 = Buy

    [Required]
    public int BHK { get; set; }

    // 🔥 Multiple Images
    [Required]
    public List<IFormFile> Images { get; set; } = new();

    // 🎥 Optional Single Video
    public IFormFile? Video { get; set; }
}

}
