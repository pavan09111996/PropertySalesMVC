namespace PropertySalesMVC.Services
{
    public interface IAuthService
    {
        Task<AdminLoginResult> ValidateAdminAsync(string adminId, string password);
    }
}
