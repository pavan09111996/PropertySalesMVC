using Microsoft.AspNetCore.Mvc;
using PropertySalesMVC.Filters;
using PropertySalesMVC.Helpers;
using PropertySalesMVC.Models;
using System.Data;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PropertySalesMVC.Controllers
{
    public class PropertyController : Controller
    {
        private readonly DbHelper _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(DbHelper db, IConfiguration configuration, ILogger<PropertyController> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<Property> list = new();

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Properties", con);
                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Property
                    {
                        //PropertyId = (int)dr["PropertyId"],
                        Title = dr["Title"].ToString(),
                        Location = dr["Location"].ToString(),
                        Price = (decimal)dr["Price"],
                        Description = dr["Description"]?.ToString(),
                        //ImageUrl = dr["ImageUrl"]?.ToString()
                    });
                }
            }


            return View(list);
        }



        [HttpGet]
        public IActionResult PropertyDetails(int id)
        {
            PropertyViewModel model = new PropertyViewModel();

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                con.Open();

                // PROPERTY
                string propertyQuery = @"SELECT Id, Title, Location, Price, Description
                                 FROM Properties
                                 WHERE Id = @Id AND IsActive = 1";

                SqlCommand cmd = new SqlCommand(propertyQuery, con);
                cmd.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                        return NotFound();

                    model.PropertyId = dr.GetInt32(0);
                    model.Title = dr.GetString(1);
                    model.Location = dr.GetString(2);
                    model.Price = dr.GetDecimal(3);
                    model.Description = dr.GetString(4);
                }

                // IMAGES
                string imageQuery = @"SELECT ImageBase64 FROM PropertyImages WHERE PropertyId = @PropertyId";
                SqlCommand imgCmd = new SqlCommand(imageQuery, con);
                imgCmd.Parameters.AddWithValue("@PropertyId", id);

                using (SqlDataReader imgDr = imgCmd.ExecuteReader())
                {
                    while (imgDr.Read())
                    {
                        model.ImagesBase64.Add(imgDr.GetString(0));
                    }
                }
            }
            var adminDetails = GetAdminDetails();

            if (adminDetails != null)
            {
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
            return View(model);
        }

        [HttpGet]
        public IActionResult EditProperty(int id)
        {
            EditPropertyViewModel model = new();

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                con.Open();

                // Get property
                string propertyQuery = "SELECT * FROM Properties WHERE Id = @Id";
                using SqlCommand cmd = new(propertyQuery, con);
                cmd.Parameters.AddWithValue("@Id", id);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return NotFound();

                model.PropertyId = id;
                model.Title = reader["Title"].ToString();
                model.Location = reader["Location"].ToString();
                model.Price = Convert.ToDecimal(reader["Price"]);
                model.Description = reader["Description"].ToString();
                reader.Close();

                // Get images
                string imageQuery = @"
                                        SELECT ImageId, ImageBase64
                                        FROM PropertyImages
                                        WHERE PropertyId = @Id";
                using SqlCommand imgCmd = new(imageQuery, con);
                imgCmd.Parameters.AddWithValue("@Id", id);

                using SqlDataReader imgReader = imgCmd.ExecuteReader();
                while (imgReader.Read())
                {
                    model.ExistingImages.Add(new PropertyImageViewModel
                    {
                        ImageId = Convert.ToInt32(imgReader["ImageId"]),
                        ImageBase64 = imgReader["ImageBase64"].ToString()
                    });
                }
            }

            return View(model);
        }

 private AdminDetails GetAdminDetails()
        {
            AdminDetails admin = null;

            using (SqlConnection con = new SqlConnection(
                "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                con.Open();

                string query = "SELECT TOP 1 * FROM AdminDetails WHERE IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        admin = new AdminDetails
                        {
                            OwnerName = reader["OwnerName"].ToString(),
                            CompanyName = reader["CompanyName"].ToString(),
                            Designation = reader["Designation"].ToString(),

                            HeadOfficeTitle = reader["HeadOfficeTitle"].ToString(),
                            HeadOfficeAddress = reader["HeadOfficeAddress"].ToString(),

                            BranchOfficeTitle = reader["BranchOfficeTitle"].ToString(),
                            BranchOfficeAddress = reader["BranchOfficeAddress"].ToString(),

                            InstagramUrl = reader["InstagramUrl"].ToString(),
                            FacebookUrl = reader["FacebookUrl"].ToString()
                        };
                    }
                }
            }

            return admin;
        }
        [HttpPost]
[AdminAuthorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProperty(EditPropertyViewModel model)
{
    model.RemoveImageIds ??= new List<int>();
    model.NewImages ??= new List<IFormFile>();

    if (!ModelState.IsValid)
    {
        model.ExistingImages = GetPropertyImages(model.PropertyId);
        return View(model);
    }

    try
    {
        await using SqlConnection con = new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));

        await con.OpenAsync();

        await using SqlTransaction tran = con.BeginTransaction();

        try
        {
            /* ===============================
               1️⃣ UPDATE PROPERTY DETAILS
            =============================== */
            await using (SqlCommand cmd = new SqlCommand(@"
                UPDATE Properties
                SET Title = @Title,
                    Location = @Location,
                    Price = @Price,
                    Description = @Description
                WHERE Id = @Id", con, tran))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.PropertyId;
                cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = model.Title;
                cmd.Parameters.Add("@Location", SqlDbType.NVarChar, 200).Value = model.Location;
                cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = model.Price;
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = model.Description ?? "";

                await cmd.ExecuteNonQueryAsync();
            }

            /* ===============================
               2️⃣ DELETE MARKED IMAGES
            =============================== */
            if (model.RemoveImageIds.Any())
            {
                await using SqlCommand deleteCmd = new SqlCommand(@"
                    DELETE FROM PropertyImages
                    WHERE ImageId = @ImageId
                      AND PropertyId = @PropertyId", con, tran);

                deleteCmd.Parameters.Add("@ImageId", SqlDbType.Int);
                deleteCmd.Parameters.Add("@PropertyId", SqlDbType.Int)
                         .Value = model.PropertyId;

                foreach (int imageId in model.RemoveImageIds.Distinct())
                {
                    deleteCmd.Parameters["@ImageId"].Value = imageId;
                    await deleteCmd.ExecuteNonQueryAsync();
                }
            }

            /* ===============================
               3️⃣ INSERT NEW IMAGES
            =============================== */
            if (model.NewImages.Any())
            {
                await using SqlCommand insertCmd = new SqlCommand(@"
                    INSERT INTO PropertyImages (PropertyId, ImageBase64)
                    VALUES (@PropertyId, @ImageBase64)", con, tran);

                insertCmd.Parameters.Add("@PropertyId", SqlDbType.Int)
                         .Value = model.PropertyId;

                insertCmd.Parameters.Add("@ImageBase64", SqlDbType.NVarChar);

                foreach (var file in model.NewImages)
                {
                    if (file == null || file.Length == 0)
                        continue;

                    // 2MB safety limit (recommended)
                    if (file.Length > 2 * 1024 * 1024)
                        throw new InvalidOperationException("Image size exceeds 2MB.");

                    await using MemoryStream ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    insertCmd.Parameters["@ImageBase64"].Value =
                        Convert.ToBase64String(ms.ToArray());

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            await tran.CommitAsync();

            TempData["PropertyUpdateMessage"] = "Property updated successfully.";
            return RedirectToAction("Dashboard", "Admin");
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update property {PropertyId}", model.PropertyId);

        model.ExistingImages = GetPropertyImages(model.PropertyId);
        ModelState.AddModelError("", "Failed to update property. Please try again.");

        return View(model);
    }
}

        private List<PropertyImageViewModel> GetPropertyImages(int propertyId)
        {
            List<PropertyImageViewModel> images = new();

            using SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;");
            con.Open();

            using SqlCommand cmd = new SqlCommand(@"
        SELECT ImageId, ImageBase64
        FROM PropertyImages
        WHERE PropertyId = @PropertyId", con);

            cmd.Parameters.AddWithValue("@PropertyId", propertyId);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                images.Add(new PropertyImageViewModel
                {
                    ImageId = (int)dr["ImageId"],
                    ImageBase64 = dr["ImageBase64"].ToString()
                });
            }

            return images;
        }

    }
}

