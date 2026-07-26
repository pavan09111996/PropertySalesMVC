using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public interface ILocationRepository
    {
        Task<List<LocationOption>> GetActiveLocationsAsync();
        Task<List<LocationOption>> GetAllLocationsOrderedAsync();
        Task<List<LocationOption>> GetAllLocationsForAdminAsync();
        Task<int> CreateLocationAsync(string name);
        Task UpdateLocationAsync(int id, string name, bool isActive);
    }
}
