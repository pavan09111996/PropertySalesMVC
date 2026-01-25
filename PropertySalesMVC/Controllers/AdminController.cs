using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertySalesMVC.Filters;
using PropertySalesMVC.Helpers;
using PropertySalesMVC.Models;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Pipelines;

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
            bool isValidAdmin = false;

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                con.Open();

                string query = @"
            SELECT COUNT(1)
            FROM AdminLoginDetails
            WHERE AdminID = @AdminID
              AND Password = @Password
              AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AdminID", adminId);
                    cmd.Parameters.AddWithValue("@Password", password);

                    isValidAdmin = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }

            if (isValidAdmin)
            {
                // ✅ CREATE SESSION
                HttpContext.Session.SetString(SessionKeys.AdminName, adminId);
                HttpContext.Session.SetString(SessionKeys.IsAdminLoggedIn, "true");

                return Json(new { success = true });
            }

            return Json(new
            {
                success = false,
                message = "Invalid Admin ID or Password"
            });
        }


        [AdminAuthorize]
public IActionResult Dashboard()
{
    var properties = new List<PropertyViewModel>();

    using (SqlConnection con = new SqlConnection(
        "Data Source=SQL6031.site4now.net,1433;" +
        "Initial Catalog=db_ac36b8_ronakrealestate00;" +
        "User ID=db_ac36b8_ronakrealestate00_admin;" +
        "Password=Ronak0910#;" +
        "Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
    {
        string query = @"
        SELECT 
            p.Id,
            p.Title,
            ISNULL(lm.Location,'') AS Location,
            p.Price,
            p.Description,
            img.ImagePath,
            img.ImageBase64
        FROM Properties p
        LEFT JOIN LocationMaster lm ON p.Location = lm.Id
        OUTER APPLY (
            SELECT TOP 1 ImagePath, ImageBase64
            FROM PropertyImages
            WHERE PropertyId = p.Id
            ORDER BY p.Id
        ) img
        WHERE p.IsActive = 1";

        SqlCommand cmd = new SqlCommand(query, con);
        con.Open();

        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var images = new List<string>();

                // ✅ Prefer ImagePath
                if (!reader.IsDBNull(5))
                {
                    images.Add(reader.GetString(5)); // ImagePath
                }
                // 🔁 Fallback to Base64
                else if (!reader.IsDBNull(6))
                {
                    images.Add("data:image/jpeg;base64," + reader.GetString(6));
                }

                properties.Add(new PropertyViewModel
                {
                    PropertyId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Location = reader.GetString(2),
                    Price = reader.GetDecimal(3),
                    Description = reader.GetString(4),
                    Images = images // 🔥 use unified Images list
                });
            }
        }
    }

    ViewBag.PropertyCount = properties.Count;
    return View(properties);
}



        [HttpGet]
        [AdminAuthorize]
        public IActionResult AddProperty()
        {
            List<LocationMaster> locationMasterList = new List<LocationMaster>();

            using (SqlConnection con = new SqlConnection(
                "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                string query = @"SELECT Id, Location FROM LocationMaster WHERE IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locationMasterList.Add(new LocationMaster
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                LocationName = reader["Location"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Locations = locationMasterList;
            return View();
        }
[HttpPost]
[AdminAuthorize]
[ValidateAntiForgeryToken]
public IActionResult AddProperty(AddPropertyViewModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] = "Please fill all required fields";
        return RedirectToAction("AddProperty");
    }

    int propertyId;

    using (SqlConnection con = new SqlConnection(
        "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;"))
    {
        string query = @"
            INSERT INTO Properties
            (Title, Location, Price, Description, LookingFor, BHK)
            OUTPUT INSERTED.Id
            VALUES
            (@Title, @Location, @Price, @Description, @LookingFor, @BHK)";

        SqlCommand cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@Title", model.Title);
        cmd.Parameters.AddWithValue("@Location", model.Location);
        cmd.Parameters.AddWithValue("@Price", model.Price);
        cmd.Parameters.AddWithValue("@Description", model.Description);
        cmd.Parameters.AddWithValue("@LookingFor", model.LookingFor);
        cmd.Parameters.AddWithValue("@BHK", model.BHK);

        con.Open();
        propertyId = (int)cmd.ExecuteScalar();

        /* ======================
           SAVE IMAGES
        ====================== */
        if (model.Images != null && model.Images.Any())
        {
            string uploadRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "properties",
                propertyId.ToString());

            Directory.CreateDirectory(uploadRoot);

            foreach (var img in model.Images)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(img.FileName);
                string fullPath = Path.Combine(uploadRoot, fileName);

                using var fs = new FileStream(fullPath, FileMode.Create);
                img.CopyTo(fs);

                string imagePath = $"/uploads/properties/{propertyId}/{fileName}";

                SqlCommand imgCmd = new SqlCommand(
                    "INSERT INTO PropertyImages (PropertyId, ImagePath) VALUES (@Pid, @Path)", con);
                imgCmd.Parameters.AddWithValue("@Pid", propertyId);
                imgCmd.Parameters.AddWithValue("@Path", imagePath);
                imgCmd.ExecuteNonQuery();
            }
        }

        /* ======================
           SAVE VIDEO (OPTIONAL)
        ====================== */
        if (model.Video != null && model.Video.Length > 0)
        {
            string videoDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "properties",
                propertyId.ToString(),
                "video");

            Directory.CreateDirectory(videoDir);

            string videoName = "property-video" + Path.GetExtension(model.Video.FileName);
            string videoPath = Path.Combine(videoDir, videoName);

            using var vs = new FileStream(videoPath, FileMode.Create);
            model.Video.CopyTo(vs);

            SqlCommand vidCmd = new SqlCommand(
                "UPDATE Properties SET VideoPath=@Video WHERE Id=@Id", con);
            vidCmd.Parameters.AddWithValue("@Video",
                $"/uploads/properties/{propertyId}/video/{videoName}");
            vidCmd.Parameters.AddWithValue("@Id", propertyId);
            vidCmd.ExecuteNonQuery();
        }
    }

    TempData["PropertyAddedMessage"] = "Property added successfully";
    return RedirectToAction("Dashboard");
}

[HttpPost]
[AdminAuthorize]
[ValidateAntiForgeryToken]
public IActionResult DeleteProperty(int propertyId)
{
    string connectionString =
        "Data Source=SQL6031.site4now.net,1433;" +
        "Initial Catalog=db_ac36b8_ronakrealestate00;" +
        "User ID=db_ac36b8_ronakrealestate00_admin;" +
        "Password=Ronak0910#;" +
        "Encrypt=False;TrustServerCertificate=True;";

    using SqlConnection con = new SqlConnection(connectionString);
    con.Open();

    using SqlTransaction tran = con.BeginTransaction();

    try
    {
        /* ===============================
           1️⃣ FETCH IMAGE + VIDEO PATHS
        =============================== */
        List<string> imagePaths = new();
        string? videoPath = null;

        // Images
        using (SqlCommand imgCmd = new SqlCommand(
            "SELECT ImagePath FROM PropertyImages WHERE PropertyId = @Id",
            con, tran))
        {
            imgCmd.Parameters.AddWithValue("@Id", propertyId);

            using SqlDataReader reader = imgCmd.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                    imagePaths.Add(reader.GetString(0));
            }
        }

        // Video
        using (SqlCommand vidCmd = new SqlCommand(
            "SELECT VideoPath FROM Properties WHERE Id = @Id",
            con, tran))
        {
            vidCmd.Parameters.AddWithValue("@Id", propertyId);
            videoPath = vidCmd.ExecuteScalar() as string;
        }

        /* ===============================
           2️⃣ DELETE FILES FROM DISK
        =============================== */
        foreach (var path in imagePaths)
        {
            string fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        if (!string.IsNullOrEmpty(videoPath))
        {
            string fullVideoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                videoPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(fullVideoPath))
                System.IO.File.Delete(fullVideoPath);
        }

        /* ===============================
           3️⃣ DELETE IMAGE RECORDS
        =============================== */
        using (SqlCommand delImagesCmd = new SqlCommand(
            "DELETE FROM PropertyImages WHERE PropertyId = @Id",
            con, tran))
        {
            delImagesCmd.Parameters.AddWithValue("@Id", propertyId);
            delImagesCmd.ExecuteNonQuery();
        }

        /* ===============================
           4️⃣ SOFT DELETE PROPERTY
        =============================== */
        using (SqlCommand delPropertyCmd = new SqlCommand(@"
            UPDATE Properties
            SET IsActive = 0,
                VideoPath = NULL
            WHERE Id = @Id", con, tran))
        {
            delPropertyCmd.Parameters.AddWithValue("@Id", propertyId);
            delPropertyCmd.ExecuteNonQuery();
        }

        /* ===============================
           5️⃣ OPTIONAL: DELETE PROPERTY FOLDER
        =============================== */
        string propertyFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "properties",
            propertyId.ToString());

        if (Directory.Exists(propertyFolder))
            Directory.Delete(propertyFolder, recursive: true);

        tran.Commit();

        TempData["PropertyDeletedMessage"] = "Property deleted successfully.";
        return RedirectToAction("Dashboard");
    }
    catch
    {
        tran.Rollback();
        throw;
    }
}


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["SuccessMessage"] = "Logged out successfully";

            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 60 * 60)] // 1 hour cache
public IActionResult PropertyImage(int propertyId)
{
    using (SqlConnection con = new SqlConnection(
        "Data Source=SQL6031.site4now.net,1433;" +
        "Initial Catalog=db_ac36b8_ronakrealestate00;" +
        "User ID=db_ac36b8_ronakrealestate00_admin;" +
        "Password=Ronak0910#;" +
        "Encrypt=False;TrustServerCertificate=True;"))
    {
        string query = @"
            SELECT TOP 1 ImageBase64
            FROM PropertyImages
            WHERE PropertyId = @PropertyId
        ";

        SqlCommand cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@PropertyId", propertyId);

        con.Open();
        var result = cmd.ExecuteScalar();

        if (result == null)
        {
            return PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/no-image.png"),
                "image/png");
        }

        byte[] bytes = Convert.FromBase64String(result.ToString());
        return File(bytes, "image/jpeg");
    }
}



    }
}
