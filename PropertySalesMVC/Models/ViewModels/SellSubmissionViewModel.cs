using System.ComponentModel.DataAnnotations;

namespace PropertySalesMVC.Models
{
    // Dedicated to the public "submit my property to sell" form (Sale.cshtml) —
    // deliberately separate from PropertyViewModel, which has non-nullable
    // reference-type fields (e.g. Location) this form doesn't collect and
    // would otherwise fail implicit-required validation for.
    public class SellSubmissionViewModel
    {
        [Required]
        public string? Title { get; set; }

        [Required]
        public int? BHK { get; set; }

        [Required]
        public decimal? Price { get; set; }

        public string? Description { get; set; }
    }
}
