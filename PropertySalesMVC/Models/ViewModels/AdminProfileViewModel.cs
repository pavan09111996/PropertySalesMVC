namespace PropertySalesMVC.Models
{
    public class AdminProfileViewModel
    {
        // Company profile (AdminDetails)
        public string? CompanyName { get; set; }
        public string? OwnerName { get; set; }
        public string? Designation { get; set; }
        public string? HeadOfficeTitle { get; set; }
        public string? HeadOfficeAddress { get; set; }
        public string? BranchOfficeTitle { get; set; }
        public string? BranchOfficeAddress { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }

        // Contact profile (AdminMaster)
        public string? AdminName { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public string? Email { get; set; }
    }
}
