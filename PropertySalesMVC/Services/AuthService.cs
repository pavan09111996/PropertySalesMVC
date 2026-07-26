using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IAdminRepository _adminRepository;

        public AuthService(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public async Task<AdminLoginResult> ValidateAdminAsync(string adminId, string password)
        {
            var record = await _adminRepository.GetLoginRecordAsync(adminId);
            if (record == null || !record.IsActive)
                return AdminLoginResult.Invalid();

            if (record.LockoutUntil.HasValue && record.LockoutUntil.Value > DateTime.UtcNow)
                return AdminLoginResult.LockedOut(record.LockoutUntil.Value);

            bool passwordValid = PasswordHasher.Verify(password, record.PasswordHash);

            if (passwordValid)
            {
                await _adminRepository.RecordLoginSuccessAsync(record.Id);
                return AdminLoginResult.Ok(record.Role);
            }

            int failedAttempts = record.FailedAttempts + 1;
            DateTime? lockoutUntil = failedAttempts >= MaxFailedAttempts
                ? DateTime.UtcNow.Add(LockoutDuration)
                : null;

            await _adminRepository.RecordLoginFailureAsync(record.Id, failedAttempts, lockoutUntil);

            return lockoutUntil.HasValue
                ? AdminLoginResult.LockedOut(lockoutUntil.Value)
                : AdminLoginResult.Invalid();
        }
    }
}
