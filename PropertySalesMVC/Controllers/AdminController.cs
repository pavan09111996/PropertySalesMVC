using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace PropertySalesMVC.Controllers
{
    public class AdminController : Controller
    {
        // ========================
        // LOGIN (AJAX)
        // ========================
        [HttpPost]
        public IActionResult Login(string adminId, string password)
        {
            // TEMP login (for testing)
            if (adminId == "admin" && password == "1234")
            {
                //HttpContext.Session.SetString("AdminLoggedIn", "true");
                return Json(new { success = true });
            }

            return Json(new
            {
                success = false,
                message = "Invalid Admin ID or Password"
            });
        }

        // ========================
        // DASHBOARD
        // ========================
        public IActionResult Dashboard()
        {
            return View();
        }

        // ========================
        // ADD PROPERTY
        // ========================
        public IActionResult AddProperty()
        {
            return View();
        }

        // ========================
        // ALL PROPERTIES
        // ========================
        public IActionResult AllProperties()
        {
            return View();
        }

        // ========================
        // LOGOUT
        // ========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
