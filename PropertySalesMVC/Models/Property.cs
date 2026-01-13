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
    public class EditPropertyViewModel
    {
        public int PropertyId { get; set; }

        public string Title { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }

        public List<PropertyImageViewModel> ExistingImages { get; set; } = new();

        // IDs of images marked for deletion
        public List<int> RemoveImageIds { get; set; } = new();

        public List<IFormFile>? NewImages { get; set; }
    }

    public class PropertyImageViewModel
    {
        public int ImageId { get; set; }
        public string ImageBase64 { get; set; }
    }
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalRecords / PageSize);

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

}
