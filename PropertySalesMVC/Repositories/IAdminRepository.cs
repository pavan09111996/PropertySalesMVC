using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public class AdminLoginRecord
    {
        public int Id { get; set; }
        public string PasswordHash { get; set; } = "";
        public int FailedAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = "Admin";
    }

    public interface IAdminRepository
    {
        Task<AdminDetails?> GetActiveAdminDetailsAsync();
        Task<AdminContactInfo?> GetActiveAdminContactAsync();

        Task<AdminLoginRecord?> GetLoginRecordAsync(string adminId);
        Task RecordLoginSuccessAsync(int loginRecordId);
        Task RecordLoginFailureAsync(int loginRecordId, int failedAttempts, DateTime? lockoutUntil);

        Task UpdateAdminDetailsAsync(AdminDetails details);
        Task UpdateAdminMasterAsync(AdminContactInfo contact);
    }
}
