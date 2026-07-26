namespace PropertySalesMVC.Models
{
    public class AdminDetails
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string OwnerName { get; set; }
        public string Designation { get; set; }

        public string HeadOfficeTitle { get; set; }
        public string HeadOfficeAddress { get; set; }

        public string BranchOfficeTitle { get; set; }
        public string BranchOfficeAddress { get; set; }

        public string InstagramUrl { get; set; }
        public string FacebookUrl { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int AdminId { get; set; }
    }
}
