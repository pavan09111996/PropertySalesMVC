using PropertySalesMVC.Models;
using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(
            IPropertyRepository propertyRepository,
            ILocationRepository locationRepository,
            IFileStorageService fileStorage,
            ILogger<PropertyService> logger)
        {
            _propertyRepository = propertyRepository;
            _locationRepository = locationRepository;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public Task<(List<PropertyViewModel> Items, int TotalCount)> GetDashboardPropertiesAsync(AdminPropertyFilter filter)
            => _propertyRepository.GetActivePropertiesForDashboardAsync(filter);

        public Task<List<LocationOption>> GetLocationsForFormAsync()
            => _locationRepository.GetActiveLocationsAsync();

        public async Task<int> AddPropertyAsync(AddPropertyViewModel model)
        {
            int propertyId = await _propertyRepository.InsertPropertyAsync(model);

            if (model.Images != null && model.Images.Any())
            {
                foreach (var img in model.Images)
                {
                    string imagePath = await _fileStorage.SavePropertyImageAsync(propertyId, img);
                    await _propertyRepository.InsertPropertyImageAsync(propertyId, imagePath);
                }
            }

            if (model.Video != null && model.Video.Length > 0)
            {
                string videoPath = await _fileStorage.SavePropertyVideoAsync(propertyId, model.Video);
                await _propertyRepository.UpdatePropertyVideoPathAsync(propertyId, videoPath);
            }

            return propertyId;
        }

        public async Task DeletePropertyAsync(int propertyId)
        {
            var (imagePaths, videoPath) = await _propertyRepository.GetPropertyFilesForDeleteAsync(propertyId);

            foreach (var path in imagePaths)
                _fileStorage.DeleteFile(path);

            if (!string.IsNullOrEmpty(videoPath))
                _fileStorage.DeleteFile(videoPath);

            await _propertyRepository.DeletePropertyCascadeAsync(propertyId);

            _fileStorage.DeletePropertyFolder(propertyId);
        }

        public async Task<PropertyViewModel?> GetPropertyDetailsAsync(int id)
        {
            var model = await _propertyRepository.GetPropertyDetailCoreAsync(id);
            if (model == null)
                return null;

            model.Images.AddRange(await _propertyRepository.GetPropertyImagePathsListAsync(id));
            return model;
        }

        public async Task<PropertyForEditResult?> GetPropertyForEditAsync(int id)
        {
            var model = await _propertyRepository.GetPropertyForEditAsync(id);
            if (model == null)
                return null;

            model.ExistingImages = await _propertyRepository.GetPropertyImagesForEditAsync(id);
            var locations = await _locationRepository.GetActiveLocationsAsync();

            return new PropertyForEditResult(model, locations);
        }

        public Task<List<PropertyImageViewModel>> GetSimplePropertyImagesAsync(int propertyId)
            => _propertyRepository.GetPropertyImagesSimpleAsync(propertyId);

        public async Task<UpdatePropertyResult> UpdatePropertyAsync(EditPropertyViewModel model)
        {
            try
            {
                // Delete files for images the user marked for removal.
                foreach (var imageId in model.RemoveImageIds.Distinct())
                {
                    var imagePath = await _propertyRepository.GetImagePathByIdAsync(imageId);
                    if (!string.IsNullOrEmpty(imagePath))
                        _fileStorage.DeleteFile(imagePath);
                }

                bool clearVideo = model.RemoveVideo;
                if (model.RemoveVideo && !string.IsNullOrEmpty(model.ExistingVideoPath))
                {
                    _fileStorage.DeleteFile(model.ExistingVideoPath);
                }

                var newImageWebPaths = new List<string>();
                foreach (var img in model.NewImages)
                {
                    if (img == null || img.Length == 0) continue;

                    if (img.Length > 5 * 1024 * 1024)
                        throw new InvalidOperationException("Image exceeds 5MB.");

                    newImageWebPaths.Add(await _fileStorage.SavePropertyImageAsync(model.PropertyId, img));
                }

                string? newVideoWebPath = null;
                if (model.NewVideo != null && model.NewVideo.Length > 0)
                {
                    if (model.NewVideo.Length > 20 * 1024 * 1024)
                        throw new InvalidOperationException("Video exceeds 20MB.");

                    if (!model.RemoveVideo && !string.IsNullOrEmpty(model.ExistingVideoPath))
                        _fileStorage.DeleteFile(model.ExistingVideoPath);

                    newVideoWebPath = await _fileStorage.SavePropertyVideoAsync(model.PropertyId, model.NewVideo);
                }

                await _propertyRepository.UpdatePropertyWithChangesAsync(
                    model,
                    model.RemoveImageIds,
                    newImageWebPaths,
                    clearVideo,
                    newVideoWebPath);

                return new UpdatePropertyResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditProperty failed for PropertyId {Id}", model.PropertyId);
                return new UpdatePropertyResult
                {
                    Success = false,
                    ErrorMessage = "Failed to update property. Please try again."
                };
            }
        }

        public Task<List<Property>> GetAllPropertiesAsync()
            => _propertyRepository.GetAllPropertiesAsync();

        public Task<List<PropertyViewModel>> GetFilteredPropertiesAsync(PropertyFilterVM filter)
        {
            filter.Mode ??= "Rent";
            return _propertyRepository.GetFilteredPropertiesAsync(filter, ResolveModeCode(filter.Mode));
        }

        public Task<int> GetTotalPropertyCountAsync(PropertyFilterVM filter)
        {
            filter.Mode ??= "Rent";
            return _propertyRepository.GetTotalPropertyCountAsync(filter, ResolveModeCode(filter.Mode));
        }

        public Task<List<LocationOption>> GetLocationsForFilterAsync()
            => _locationRepository.GetAllLocationsOrderedAsync();

        public Task<List<PropertyViewModel>> GetFeaturedPropertiesAsync()
            => _propertyRepository.GetFeaturedPropertiesAsync();

        private static int ResolveModeCode(string? mode) => mode switch
        {
            "Buy" => (int)PropertyMode.Buy,
            "Rent" => (int)PropertyMode.Rent,
            _ => (int)PropertyMode.Sell
        };
    }
}
