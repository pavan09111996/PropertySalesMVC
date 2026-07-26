using Microsoft.AspNetCore.Mvc;
using PropertySalesMVC.Filters;
using PropertySalesMVC.Helpers;
using PropertySalesMVC.Models;
using PropertySalesMVC.Services;

namespace PropertySalesMVC.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;
        private readonly IAdminService _adminService;
        private readonly IEnquiryService _enquiryService;

        public PropertyController(IPropertyService propertyService, IAdminService adminService, IEnquiryService enquiryService)
        {
            _propertyService = propertyService;
            _adminService = adminService;
            _enquiryService = enquiryService;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _propertyService.GetAllPropertiesAsync();

            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> PropertyDetails(int id)
        {
            var model = await _propertyService.GetPropertyDetailsAsync(id);
            if (model == null) return NotFound();

            var admin = await _adminService.GetActiveAdminContactAsync();
            ViewBag.WhatsAppNumber = admin?.WhatsApp ?? "";

            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogEnquiry(int propertyId)
        {
            await _enquiryService.LogPropertyInquiryAsync(propertyId);
            return Ok();
        }

        [HttpGet]
        [AdminAuthorize]
        // on click of Edit
        public async Task<IActionResult> EditProperty(int id)
        {
            var result = await _propertyService.GetPropertyForEditAsync(id);
            if (result == null) return NotFound();

            ViewBag.Locations = result.Locations;
            return View(result.Model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        public async Task<IActionResult> EditProperty(EditPropertyViewModel model)
        {
            model.RemoveImageIds ??= new List<int>();
            model.NewImages ??= new List<IFormFile>();

            if (model.NewVideo != null && model.NewVideo.Length > UploadLimits.MaxVideoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.NewVideo), "Video must be under 20MB — please compress it or trim the clip and try again.");
            }

            if (!ModelState.IsValid)
            {
                model.ExistingImages = await _propertyService.GetSimplePropertyImagesAsync(model.PropertyId);
                ViewBag.Locations = await _propertyService.GetLocationsForFormAsync();
                return View(model);
            }

            var result = await _propertyService.UpdatePropertyAsync(model);

            if (!result.Success)
            {
                model.ExistingImages = await _propertyService.GetSimplePropertyImagesAsync(model.PropertyId);
                ViewBag.Locations = await _propertyService.GetLocationsForFormAsync();
                ModelState.AddModelError("", result.ErrorMessage ?? "Failed to update property. Please try again.");
                return View(model);
            }

            TempData["PropertyUpdateMessage"] = "Property updated successfully.";
            return RedirectToAction("Dashboard", "Admin");
        }

        public async Task<IActionResult> Listing(PropertyFilterVM filter)
        {
            filter.Mode ??= "Rent";

            var properties = await _propertyService.GetFilteredPropertiesAsync(filter);

            ViewBag.Mode = filter.Mode;
            ViewBag.Locations = await _propertyService.GetLocationsForFilterAsync();
            ViewBag.SelectedLocation = filter.LocationId;
            ViewBag.SelectedBHK = filter.BHK;

            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            return View(properties);
        }

        public Task<IActionResult> Rent(int? locationId, int? bhk)
            => Listing(new PropertyFilterVM { Mode = "Rent", LocationId = locationId, BHK = bhk });

        public Task<IActionResult> Buy(int? locationId, int? bhk)
            => Listing(new PropertyFilterVM { Mode = "Buy", LocationId = locationId, BHK = bhk });

        public Task<IActionResult> Sell(int? locationId, int? bhk)
            => Listing(new PropertyFilterVM { Mode = "Sell", LocationId = locationId, BHK = bhk });

        [HttpGet]
        public async Task<IActionResult> ListingPartial(PropertyFilterVM filter)
        {
            filter.Mode ??= "Rent";

            filter.Page = filter.Page <= 0 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize <= 0 ? 9 : filter.PageSize;

            var properties = await _propertyService.GetFilteredPropertiesAsync(filter);
            var totalCount = await _propertyService.GetTotalPropertyCountAsync(filter);

            ViewBag.CurrentPage = filter.Page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            return PartialView("_PropertyGridWithPagination", properties);
        }

        public async Task<IActionResult> FilterPartial(string mode)
        {
            ViewBag.Locations = await _propertyService.GetLocationsForFilterAsync();
            ViewBag.Mode = mode;

            return PartialView("_PropertyFilter");
        }

        [HttpGet]
        public async Task<IActionResult> Sale()
        {
            ViewBag.Locations = await _propertyService.GetLocationsForFilterAsync();

            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            return View("Sale");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sale(SellSubmissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill all required fields correctly.";
                return RedirectToAction("Sale");
            }

            // Persist the lead so admin has a record of it beyond the WhatsApp thread
            await _enquiryService.LogSellSubmissionAsync(model.Title, model.BHK, model.Price, model.Description);

            // Prepare WhatsApp message — number comes from the active AdminMaster record
            var admin = await _adminService.GetActiveAdminContactAsync();
            string phoneNumber = admin?.WhatsApp ?? "";

            string messageText =
                $@"Hello Ronak Estate 👋

                I would like to sell my property 🏠

                Property Details
                Title: {model.Title}
                BHK: {model.BHK}
                Expected Price: ₹{model.Price!.Value.ToString("N0", new System.Globalization.CultureInfo("en-IN"))}

                Please contact me for further discussion.
                Thank you 🙂";

            string message = Uri.EscapeDataString(messageText);

            // Redirect to WhatsApp
            return Redirect($"https://wa.me/{phoneNumber}?text={message}");
        }

        [HttpGet]
        public async Task<IActionResult> SalePartial()
        {
            ViewBag.Locations = await _propertyService.GetLocationsForFilterAsync();

            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            return PartialView("Sale");
        }

        private void PopulateAdminViewBag(AdminDetails? adminDetails)
        {
            if (adminDetails == null)
                return;

            ViewBag.CompanyName = adminDetails.CompanyName;
            ViewBag.OwnerName = adminDetails.OwnerName;
            ViewBag.Designation = adminDetails.Designation;

            ViewBag.HeadOfficeTitle = adminDetails.HeadOfficeTitle;
            ViewBag.HeadOfficeAddress = adminDetails.HeadOfficeAddress;

            ViewBag.BranchOfficeTitle = adminDetails.BranchOfficeTitle;
            ViewBag.BranchOfficeAddress = adminDetails.BranchOfficeAddress;

            ViewBag.InstagramUrl = adminDetails.InstagramUrl;
            ViewBag.FacebookUrl = adminDetails.FacebookUrl;
        }
    }
}
