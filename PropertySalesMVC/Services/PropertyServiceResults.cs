using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public class PropertyForEditResult
    {
        public PropertyForEditResult(EditPropertyViewModel model, List<LocationOption> locations)
        {
            Model = model;
            Locations = locations;
        }

        public EditPropertyViewModel Model { get; }
        public List<LocationOption> Locations { get; }
    }

    public class UpdatePropertyResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
