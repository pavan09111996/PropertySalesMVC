using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public interface IEnquiryRepository
    {
        Task InsertEnquiryAsync(NewEnquiry enquiry);
        Task<List<Enquiry>> GetEnquiriesAsync(int page, int pageSize);
        Task<int> GetEnquiryCountAsync();
    }
}
