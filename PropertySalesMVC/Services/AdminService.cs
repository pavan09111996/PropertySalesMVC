using PropertySalesMVC.Models;
using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        public AdminService(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public Task<AdminDetails?> GetAdminDetailsForLayoutAsync()
            => _adminRepository.GetActiveAdminDetailsAsync();

        public Task<AdminContactInfo?> GetActiveAdminContactAsync()
            => _adminRepository.GetActiveAdminContactAsync();

        public Task UpdateAdminDetailsAsync(AdminDetails details)
            => _adminRepository.UpdateAdminDetailsAsync(details);

        public Task UpdateAdminContactAsync(AdminContactInfo contact)
            => _adminRepository.UpdateAdminMasterAsync(contact);
    }
}
