using PropertySalesMVC.Models;

namespace PropertySalesMVC.Services
{
    public interface IEnquiryService
    {
        Task LogContactEnquiryAsync(string name, string phone, string message);
        Task LogPropertyInquiryAsync(int propertyId);
        Task LogSellSubmissionAsync(string? title, int? bhk, decimal? price, string? description);

        Task<(List<Enquiry> Enquiries, int TotalCount)> GetEnquiriesAsync(int page, int pageSize);
    }
}
