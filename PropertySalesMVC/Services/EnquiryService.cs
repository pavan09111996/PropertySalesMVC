using PropertySalesMVC.Models;
using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class EnquiryService : IEnquiryService
    {
        private readonly IEnquiryRepository _enquiryRepository;

        public EnquiryService(IEnquiryRepository enquiryRepository)
        {
            _enquiryRepository = enquiryRepository;
        }

        public Task LogContactEnquiryAsync(string name, string phone, string message)
            => _enquiryRepository.InsertEnquiryAsync(new NewEnquiry
            {
                EnquiryType = "Contact",
                Name = name,
                Phone = phone,
                Message = message
            });

        public Task LogPropertyInquiryAsync(int propertyId)
            => _enquiryRepository.InsertEnquiryAsync(new NewEnquiry
            {
                EnquiryType = "PropertyInquiry",
                PropertyId = propertyId
            });

        public Task LogSellSubmissionAsync(string? title, int? bhk, decimal? price, string? description)
            => _enquiryRepository.InsertEnquiryAsync(new NewEnquiry
            {
                EnquiryType = "SellSubmission",
                Title = title,
                BHK = bhk,
                Price = price,
                Description = description
            });

        public async Task<(List<Enquiry> Enquiries, int TotalCount)> GetEnquiriesAsync(int page, int pageSize)
        {
            var enquiries = await _enquiryRepository.GetEnquiriesAsync(page, pageSize);
            var total = await _enquiryRepository.GetEnquiryCountAsync();
            return (enquiries, total);
        }
    }
}
