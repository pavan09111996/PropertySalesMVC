using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public interface ILocationService
    {
        Task<List<LocationOption>> GetAllForAdminAsync();
        Task CreateAsync(string name);
        Task UpdateAsync(int id, string name, bool isActive);
    }
}
