using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public interface IPropertyRepository
    {
        Task<(List<PropertyViewModel> Items, int TotalCount)> GetActivePropertiesForDashboardAsync(AdminPropertyFilter filter);

        Task<int> InsertPropertyAsync(AddPropertyViewModel model);
        Task InsertPropertyImageAsync(int propertyId, string imagePath);
        Task UpdatePropertyVideoPathAsync(int propertyId, string videoPath);

        Task<(List<string> ImagePaths, string? VideoPath)> GetPropertyFilesForDeleteAsync(int propertyId);
        Task DeletePropertyCascadeAsync(int propertyId);

        Task<PropertyViewModel?> GetPropertyDetailCoreAsync(int id);
        Task<List<string>> GetPropertyImagePathsListAsync(int id);

        Task<EditPropertyViewModel?> GetPropertyForEditAsync(int id);
        Task<List<PropertyImageViewModel>> GetPropertyImagesForEditAsync(int id);
        Task<List<PropertyImageViewModel>> GetPropertyImagesSimpleAsync(int propertyId);
        Task<string?> GetImagePathByIdAsync(int imageId);

        Task UpdatePropertyWithChangesAsync(
            EditPropertyViewModel model,
            List<int> imageIdsToDelete,
            List<string> newImageWebPaths,
            bool clearVideo,
            string? newVideoWebPath);

        Task<List<Property>> GetAllPropertiesAsync();

        Task<List<PropertyViewModel>> GetFilteredPropertiesAsync(PropertyFilterVM filter, int modeCode);
        Task<int> GetTotalPropertyCountAsync(PropertyFilterVM filter, int modeCode);

        Task<List<PropertyViewModel>> GetFeaturedPropertiesAsync();
    }
}
