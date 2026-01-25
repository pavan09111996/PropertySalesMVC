using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
