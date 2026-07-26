using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public interface IAdminService
    {
        Task<AdminDetails?> GetAdminDetailsForLayoutAsync();
        Task<AdminContactInfo?> GetActiveAdminContactAsync();
        Task UpdateAdminDetailsAsync(AdminDetails details);
        Task UpdateAdminContactAsync(AdminContactInfo contact);
    }
}
