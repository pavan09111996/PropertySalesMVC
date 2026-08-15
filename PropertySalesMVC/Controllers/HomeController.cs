using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PropertySalesMVC.Models;
using PropertySalesMVC.Services;

namespace PropertySalesMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAdminService _adminService;
        private readonly IPropertyService _propertyService;

        public HomeController(ILogger<HomeController> logger, IAdminService adminService, IPropertyService propertyService)
        {
            _logger = logger;
            _adminService = adminService;
            _propertyService = propertyService;
        }

        public async Task<IActionResult> Index()
        {
            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            var contact = await _adminService.GetActiveAdminContactAsync();
            ViewBag.WhatsAppNumber = contact?.WhatsApp ?? "";

            // Panel shows ONLY explicitly-Featured properties — no fallback
            // to recent listings. The hero panel itself is always rendered
            // (see Home/Index.cshtml); when nothing's ticked it shows its
            // own decorative motif instead of the listings track.
            var featured = await _propertyService.GetFeaturedPropertiesAsync();
            return View(featured);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            ViewBag.Admin = await _adminService.GetActiveAdminContactAsync();

            var adminDetails = await _adminService.GetAdminDetailsForLayoutAsync();
            PopulateAdminViewBag(adminDetails);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _adminService.GetActiveAdminContactAsync();

            if (admin == null)
            {
                ModelState.AddModelError("", "Contact service temporarily unavailable.");
                return View(model);
            }

            string message =
                "New Property Enquiry\n" +
                "----------------------\n" +
                $"Name: {model.Name}\n" +
                $"Phone: {model.Phone}\n" +
                "Customer Message:\n" +
                $"{model.Message}\n\n" +
                $"Assigned To: {admin.AdminName}\n" +
                "Source: Website";

            string whatsappUrl =
                $"https://wa.me/{admin.WhatsApp}?text={Uri.EscapeDataString(message)}";

            return Redirect(whatsappUrl);
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
