namespace PropertySalesMVC.Services
{
    public class AdminLoginResult
    {
        public bool Success { get; init; }
        public bool IsLockedOut { get; init; }
        public string? Message { get; init; }
        public string? Role { get; init; }

        public static AdminLoginResult Ok(string role) => new() { Success = true, Role = role };

        public static AdminLoginResult Invalid() => new()
        {
            Success = false,
            Message = "Invalid Admin ID or Password"
        };

        public static AdminLoginResult LockedOut(DateTime lockoutUntilUtc) => new()
        {
            Success = false,
            IsLockedOut = true,
            Message = $"Too many failed attempts. Try again after {lockoutUntilUtc.ToLocalTime():t}."
        };
    }
}
