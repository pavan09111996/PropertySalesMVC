namespace PropertySalesMVC.Models
{
    public class ContactViewModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Message { get; set; }
    }

    public class AdminContactInfo
    {
        public int AdminId { get; set; }
        public string AdminName { get; set; }
        public string Phone { get; set; }
        public string WhatsApp { get; set; }
        public string Email { get; set; }
    }


}