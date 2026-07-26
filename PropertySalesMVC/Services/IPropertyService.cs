using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public interface IPropertyService
    {
        Task<(List<PropertyViewModel> Items, int TotalCount)> GetDashboardPropertiesAsync(AdminPropertyFilter filter);
        Task<List<LocationOption>> GetLocationsForFormAsync();
        Task<int> AddPropertyAsync(AddPropertyViewModel model);
        Task DeletePropertyAsync(int propertyId);

        Task<PropertyViewModel?> GetPropertyDetailsAsync(int id);

        Task<PropertyForEditResult?> GetPropertyForEditAsync(int id);
        Task<UpdatePropertyResult> UpdatePropertyAsync(EditPropertyViewModel model);
        Task<List<PropertyImageViewModel>> GetSimplePropertyImagesAsync(int propertyId);

        Task<List<Property>> GetAllPropertiesAsync();

        Task<List<PropertyViewModel>> GetFilteredPropertiesAsync(PropertyFilterVM filter);
        Task<int> GetTotalPropertyCountAsync(PropertyFilterVM filter);
        Task<List<LocationOption>> GetLocationsForFilterAsync();

        Task<List<PropertyViewModel>> GetFeaturedPropertiesAsync();
    }
}
