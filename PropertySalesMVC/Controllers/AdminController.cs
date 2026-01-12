using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PropertySalesMVC.Helpers;
using System.Data.SqlClient;

namespace PropertySalesMVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly DbHelper _db;  
        public AdminController(DbHelper db)
        {
            _db = db;
        }


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

        [HttpGet]
        public IActionResult AddProperty()
        {
            return View();
        }


        [HttpPost]
        public IActionResult AddProperty(
            string Title,
            string Location,
            decimal Price,
            string Description,
            List<IFormFile> Images)
        {
           
            // SAVE IMAGES
            
            List<string> imageNames = new List<string>();
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            if (Images != null && Images.Count > 0)
            {
                foreach (var img in Images)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(img.FileName);
                    string filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        img.CopyTo(stream);
                    }

                    imageNames.Add(fileName);
                }
            }

            string imagePaths = string.Join(",", imageNames);


            // SAVE TO DATABASE

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                string query = @"INSERT INTO Properties
                        (Title, Location, Price, Description, ImagePaths)
                        VALUES
                        (@Title, @Location, @Price, @Description, @ImagePaths)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Title", Title);
                cmd.Parameters.AddWithValue("@Location", Location);
                cmd.Parameters.AddWithValue("@Price", Price);
                cmd.Parameters.AddWithValue("@Description", Description);
                cmd.Parameters.AddWithValue("@ImagePaths", imagePaths);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }

            return RedirectToAction("AllProperties");
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
