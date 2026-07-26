namespace PropertySalesMVC.Services
{
    public interface IFileStorageService
    {
        Task<string> SavePropertyImageAsync(int propertyId, IFormFile file);
        Task<string> SavePropertyVideoAsync(int propertyId, IFormFile file);
        void DeleteFile(string webRelativePath);
        void DeletePropertyFolder(int propertyId);
    }
}
