using Microsoft.AspNetCore.Mvc;
using PropertySalesMVC.Filters;
using PropertySalesMVC.Helpers;
using PropertySalesMVC.Models;
using PropertySalesMVC.Services;

namespace PropertySalesMVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IPropertyService _propertyService;
        private readonly ILocationService _locationService;
        private readonly IAdminService _adminService;
        private readonly IErrorLogService _errorLogService;
        private readonly IAnalyticsService _analyticsService;

        public AdminController(
            IAuthService authService,
            IPropertyService propertyService,
            ILocationService locationService,
            IAdminService adminService,
            IErrorLogService errorLogService,
            IAnalyticsService analyticsService)
        {
            _authService = authService;
            _propertyService = propertyService;
            _locationService = locationService;
            _adminService = adminService;
            _errorLogService = errorLogService;
            _analyticsService = analyticsService;
        }

        [AdminAuthorize]
        public async Task<IActionResult> Analytics(int days = 30)
        {
            // Pin to the handful of ranges the view's tabs offer — an
            // arbitrary ?days= shouldn't run an unbounded query.
            if (days != 7 && days != 30 && days != 90)
                days = 30;

            var model = await _analyticsService.GetAnalyticsAsync(days);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(string adminId, string password)
        {
            var result = await _authService.ValidateAdminAsync(adminId, password);

            if (result.Success)
            {
                HttpContext.Session.SetString(SessionKeys.AdminName, adminId);
                HttpContext.Session.SetString(SessionKeys.IsAdminLoggedIn, "true");
                HttpContext.Session.SetString(SessionKeys.Role, result.Role ?? Roles.Admin);

                return Json(new { success = true, role = result.Role });
            }

            return Json(new
            {
                success = false,
                message = result.Message
            });
        }

        [AdminAuthorize]
        public async Task<IActionResult> Dashboard(string? search, int? lookingFor, int page = 1)
        {
            var filter = new AdminPropertyFilter
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                LookingFor = lookingFor,
                Page = page <= 0 ? 1 : page
            };

            var (items, totalCount) = await _propertyService.GetDashboardPropertiesAsync(filter);

            ViewBag.PropertyCount = totalCount;
            ViewBag.Search = filter.Search;
            ViewBag.LookingFor = filter.LookingFor;
            ViewBag.CurrentPage = filter.Page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            return View(items);
        }

        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> AddProperty()
        {
            ViewBag.Locations = await _propertyService.GetLocationsForFormAsync();
            return View();
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProperty(AddPropertyViewModel model)
        {
            if (model.Video != null && model.Video.Length > UploadLimits.MaxVideoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.Video), "Video must be under 20MB — please compress it or trim the clip and try again.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Locations = await _propertyService.GetLocationsForFormAsync();
                return View(model);
            }

            await _propertyService.AddPropertyAsync(model);

            TempData["PropertyAddedMessage"] = "Property added successfully";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProperty(int propertyId)
        {
            await _propertyService.DeletePropertyAsync(propertyId);

            TempData["PropertyDeletedMessage"] = "Property permanently deleted.";
            return RedirectToAction("Dashboard");
        }

        [AdminAuthorize]
        public async Task<IActionResult> Locations()
        {
            var locations = await _locationService.GetAllForAdminAsync();
            return View(locations);
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLocation(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _locationService.CreateAsync(name.Trim());
                TempData["LocationMessage"] = "Location added.";
            }

            return RedirectToAction("Locations");
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLocation(int id, string name, bool isActive)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _locationService.UpdateAsync(id, name.Trim(), isActive);
                TempData["LocationMessage"] = "Location updated.";
            }

            return RedirectToAction("Locations");
        }

        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Profile()
        {
            var details = await _adminService.GetAdminDetailsForLayoutAsync();
            var contact = await _adminService.GetActiveAdminContactAsync();

            var model = new AdminProfileViewModel
            {
                CompanyName = details?.CompanyName,
                OwnerName = details?.OwnerName,
                Designation = details?.Designation,
                HeadOfficeTitle = details?.HeadOfficeTitle,
                HeadOfficeAddress = details?.HeadOfficeAddress,
                BranchOfficeTitle = details?.BranchOfficeTitle,
                BranchOfficeAddress = details?.BranchOfficeAddress,
                InstagramUrl = details?.InstagramUrl,
                FacebookUrl = details?.FacebookUrl,

                AdminName = contact?.AdminName,
                Phone = contact?.Phone,
                WhatsApp = contact?.WhatsApp,
                Email = contact?.Email
            };

            return View(model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AdminProfileViewModel model)
        {
            await _adminService.UpdateAdminDetailsAsync(new AdminDetails
            {
                CompanyName = model.CompanyName ?? "",
                OwnerName = model.OwnerName ?? "",
                Designation = model.Designation ?? "",
                HeadOfficeTitle = model.HeadOfficeTitle ?? "",
                HeadOfficeAddress = model.HeadOfficeAddress ?? "",
                BranchOfficeTitle = model.BranchOfficeTitle ?? "",
                BranchOfficeAddress = model.BranchOfficeAddress ?? "",
                InstagramUrl = model.InstagramUrl ?? "",
                FacebookUrl = model.FacebookUrl ?? ""
            });

            await _adminService.UpdateAdminContactAsync(new AdminContactInfo
            {
                AdminName = model.AdminName ?? "",
                Phone = model.Phone ?? "",
                WhatsApp = model.WhatsApp ?? "",
                Email = model.Email ?? ""
            });

            TempData["ProfileMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        [DeveloperAuthorize]
        public async Task<IActionResult> ErrorLogs(int page = 1)
        {
            var (entries, totalCount) = await _errorLogService.GetErrorLogsAsync(page <= 0 ? 1 : page, 20);

            ViewBag.CurrentPage = page <= 0 ? 1 : page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 20.0);

            return View(entries);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["SuccessMessage"] = "Logged out successfully";

            return RedirectToAction("Index", "Home");
        }
    }
}
