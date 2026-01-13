using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertySalesMVC.Helpers;
using PropertySalesMVC.Models;
using System.Data.SqlClient;
using System.Diagnostics;

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


        public IActionResult Dashboard()
        {
            var properties = new Dictionary<int, PropertyViewModel>();

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                string query = @"
            SELECT 
                p.Id,
                p.Title,
                p.Location,
                p.Price,
                p.Description,
                i.ImageBase64
                FROM Properties p
                LEFT JOIN PropertyImages i
                ON p.Id = i.PropertyId
                where p.IsActive = 1
                 ";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int propertyId = reader.GetInt32(0);

                        // Create parent once
                        if (!properties.ContainsKey(propertyId))
                        {
                            properties[propertyId] = new PropertyViewModel
                            {
                                PropertyId = propertyId,
                                Title = reader.GetString(1),
                                Location = reader.GetString(2),
                                Price = reader.GetDecimal(3),
                                Description = reader.GetString(4)
                            };
                        }

                        // Add images (can be null because of LEFT JOIN)
                        if (!reader.IsDBNull(5))
                        {
                            properties[propertyId]
                                .ImagesBase64
                                .Add(reader.GetString(5));
                        }
                    }
                }
            }
            var propertyCount = properties.Count;
            ViewBag.PropertyCount = propertyCount;
            return View(properties.Values.ToList());
        }

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
            List<string> base64Images = new List<string>();

            if (Images != null && Images.Any())
            {
                foreach (var img in Images)
                {
                    using (var ms = new MemoryStream())
                    {
                        img.CopyTo(ms);
                        byte[] imageBytes = ms.ToArray();
                        string base64String = Convert.ToBase64String(imageBytes);

                        base64Images.Add(base64String);
                    }
                }
            }

            using (SqlConnection con = new SqlConnection(
                "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                string query = @"INSERT INTO Properties (Title, Location, Price, Description)
                                OUTPUT INSERTED.Id
                                VALUES (@Title, @Location, @Price, @Description)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Title", Title);
                cmd.Parameters.AddWithValue("@Location", Location);
                cmd.Parameters.AddWithValue("@Price", Price);
                cmd.Parameters.AddWithValue("@Description", Description);

                con.Open();

                int propertyId = (int)cmd.ExecuteScalar();

                #region Insert images
                //  Insert Images (Base64)
                if (base64Images != null && base64Images.Any())
                {
                    foreach (var img in base64Images)
                    {

                        string imageQuery = @"
                            INSERT INTO PropertyImages (PropertyId, ImageBase64)
                            VALUES (@PropertyId, @ImageBase64)";

                        SqlCommand imageCmd = new SqlCommand(imageQuery, con);
                        imageCmd.Parameters.AddWithValue("@PropertyId", propertyId);
                        imageCmd.Parameters.AddWithValue("@ImageBase64", img);

                        imageCmd.ExecuteNonQuery();
                    }
                }
                #endregion
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProperty(int propertyId)
        {
            using (SqlConnection con = new SqlConnection(
                "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;"))
            {
                string query = @"
                                UPDATE Properties 
                                SET IsActive = 0 
                                WHERE Id = @PropertyId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Dashboard");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
