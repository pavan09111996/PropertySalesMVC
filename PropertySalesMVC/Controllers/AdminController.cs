using Microsoft.AspNetCore.Mvc;

namespace PropertySalesMVC.Controllers
{
    public class AdminController : Controller
    {
        [HttpPost]
        public IActionResult Login(string adminId, string password)
        {
            // TEMP login (for testing)
            if (adminId == "admin" && password == "1234")
            {

                HttpContext.Session.SetString("AdminLoggedIn", "true");
                return Json(new { success = true });
                
            }

            return Json(new
            {
                success = false,
                message = "Invalid Admin ID or Password"
            });
        }



        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult AddProperty()
        {
            return View();
        }

        public IActionResult AllProperties()
        {
            return View();
        }    
       


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
