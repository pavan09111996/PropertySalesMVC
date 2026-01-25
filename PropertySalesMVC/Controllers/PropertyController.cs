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
                string propertyQuery = @"
                    SELECT p.Id, p.Title, isnull(lm.Location,'') as Location, p.Price, p.Description,p.bhk 
                    FROM Properties p
                    LEFT JOIN LocationMaster lm
                    on p.Location = lm.id
                    WHERE p.Id = @Id AND p.IsActive = 1
                     ";

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
                    model.BHK = dr.GetInt32(5);
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
        //on click of Edit
        public IActionResult EditProperty(int id)
        {
            EditPropertyViewModel model = new();

            using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                con.Open();

                /* =======================
                   1️⃣ GET PROPERTY DETAILS
                   ======================= */
                string propertyQuery = @"
            SELECT 
                Id,
                Title,
                Location,
                Price,
                Description,
                LookingFor,
                BHK
            FROM Properties
            WHERE Id = @Id";

                using SqlCommand cmd = new(propertyQuery, con);
                cmd.Parameters.AddWithValue("@Id", id);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return NotFound();

                model.PropertyId = id;
                model.Title = reader["Title"].ToString();
                model.LocationId = Convert.ToInt32(reader["Location"]);
                model.Price = Convert.ToDecimal(reader["Price"]);
                model.Description = reader["Description"].ToString();
                model.LookingFor = Convert.ToInt32(reader["LookingFor"]);
                model.BHK = Convert.ToInt32(reader["BHK"]);

                reader.Close();

                /* =======================
                   2️⃣ GET PROPERTY IMAGES
                   ======================= */
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
                imgReader.Close();

                /* =======================
                   3️⃣ GET LOCATIONS (DROPDOWN)
                   TABLE: locationmaster
                   ======================= */
                List<LocationViewModel> locations = new();

                string locQuery = @"
    SELECT 
        Id,
        Location AS LocationName
    FROM locationmaster
    WHERE isActive = 1
";

                using SqlCommand locCmd = new(locQuery, con);
                using SqlDataReader locReader = locCmd.ExecuteReader();

                while (locReader.Read())
                {
                    locations.Add(new LocationViewModel
                    {
                        Id = Convert.ToInt32(locReader["Id"]),
                        LocationName = locReader["LocationName"].ToString()
                    });
                }

                ViewBag.Locations = locations;
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
                // Use your connection string directly
                string connectionString = "Data Source=SQL6031.site4now.net,1433;" +
                                          "Initial Catalog=db_ac36b8_ronakrealestate00;" +
                                          "User ID=db_ac36b8_ronakrealestate00_admin;" +
                                          "Password=Ronak0910#;" +
                                          "Encrypt=False;TrustServerCertificate=True;Connect Timeout=30;";

                await using SqlConnection con = new SqlConnection(connectionString);
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
                    Description = @Description,
                    LookingFor = @LookingFor,
                    BHK = @BHK
                WHERE Id = @Id", con, tran))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = model.PropertyId;
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = model.Title;
                        cmd.Parameters.Add("@Location", SqlDbType.Int).Value = model.LocationId;   // INT ✅
                        cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = model.Price;
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = model.Description ?? "";
                        cmd.Parameters.Add("@LookingFor", SqlDbType.Int).Value = model.LookingFor;
                        cmd.Parameters.Add("@BHK", SqlDbType.Int).Value = model.BHK;

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

                            // 2MB safety limit
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

        /* ===============================
           HELPER TO GET EXISTING IMAGES
           =============================== */
        private List<PropertyImageViewModel> GetPropertyImages(int propertyId)
        {
            List<PropertyImageViewModel> images = new();

            string connectionString = "Data Source=SQL6031.site4now.net,1433;" +
                                      "Initial Catalog=db_ac36b8_ronakrealestate00;" +
                                      "User ID=db_ac36b8_ronakrealestate00_admin;" +
                                      "Password=Ronak0910#;" +
                                      "Encrypt=False;TrustServerCertificate=True;Connect Timeout=30;";

            using SqlConnection con = new SqlConnection(connectionString);
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






        public List<LocationMasterNew> GetLocations()
        {
            var locations = new List<LocationMasterNew>();

            string connectionString =
                "Data Source=SQL6031.site4now.net,1433;" +
                "Initial Catalog=db_ac36b8_ronakrealestate00;" +
                "User ID=db_ac36b8_ronakrealestate00_admin;" +
                "Password=Ronak0910#;" +
                "Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, Location FROM LocationMaster ORDER BY Location";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locations.Add(new LocationMasterNew
                            {
                                Id = reader["Id"].ToString(),
                                Location = reader["Location"].ToString()
                            });
                        }
                    }
                }
            }

            return locations;
        }




        

public IActionResult Listing(PropertyFilterVM filter)
{
    // default mode
    filter.Mode ??= "Rent";

    var properties = GetFilteredProperties(filter);

    ViewBag.Mode = filter.Mode;
    ViewBag.Locations = GetLocations(); // dropdown
    ViewBag.SelectedLocation = filter.LocationId;
    ViewBag.SelectedBHK = filter.BHK;

    return View(properties);
}


public IActionResult Rent(int? locationId, int? bhk)
{
    return Listing(new PropertyFilterVM
    {
        Mode = "Rent",
        LocationId = locationId,
        BHK = bhk
    });
}

public IActionResult Buy(int? locationId, int? bhk)
{
    return Listing(new PropertyFilterVM
    {
        Mode = "Buy",
        LocationId = locationId,
        BHK = bhk
    });
}

public IActionResult Sell(int? locationId, int? bhk)
{
    return Listing(new PropertyFilterVM
    {
        Mode = "Sell",
        LocationId = locationId,
        BHK = bhk
    });
}



[HttpGet]
public IActionResult ListingPartial(PropertyFilterVM filter)
{
    filter.Mode ??= "Rent";

    // 🔥 FORCE Page + PageSize
    filter.Page = filter.Page <= 0 ? 1 : filter.Page;
    filter.PageSize = filter.PageSize <= 0 ? 9 : filter.PageSize;

    var properties = GetFilteredProperties(filter);
    var totalCount = GetTotalPropertyCount(filter);

    ViewBag.CurrentPage = filter.Page;
    ViewBag.TotalPages =
        (int)Math.Ceiling(totalCount / (double)filter.PageSize);

    return PartialView("_PropertyGridWithPagination", properties);
}



public IActionResult FilterPartial(string mode)
{
    ViewBag.Locations = GetLocations();
    ViewBag.Mode = mode;

    return PartialView("_PropertyFilter");
}


public List<PropertyViewModel> GetFilteredProperties(PropertyFilterVM filter)
    {
        var properties = new Dictionary<int, PropertyViewModel>();

        using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;" +
                                      "Initial Catalog=db_ac36b8_ronakrealestate00;" +
                                      "User ID=db_ac36b8_ronakrealestate00_admin;" +
                                      "Password=Ronak0910#;" +
                                      "Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
        {
            string query = @"
                SELECT
                    p.Id,
                    p.Title,
                    p.Price,
                    p.Description,
                    p.BHK,
                    lm.Id AS LocationId,
                    lm.Location
                FROM Properties p
                JOIN LocationMaster lm ON p.Location = lm.Id
                WHERE p.IsActive = 1
                  AND p.LookingFor = @Mode
                  AND (@LocationId IS NULL OR lm.Id = @LocationId)
                  AND (@BHK IS NULL OR p.BHK = @BHK)
                ORDER BY p.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            ";

            SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Mode", filter.Mode == "Buy" ? 2 :
                filter.Mode == "Rent" ? 1 : 3);
            cmd.Parameters.AddWithValue("@LocationId",
                (object?)filter.LocationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BHK",
                (object?)filter.BHK ?? DBNull.Value);
cmd.Parameters.AddWithValue("@Offset",
    (filter.Page - 1) * filter.PageSize);

cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);
            con.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int propertyId = reader.GetInt32(0);

                    properties[propertyId] = new PropertyViewModel
                    {
                        PropertyId = propertyId,
                        Title = reader.GetString(1),
                        Price = reader.GetDecimal(2),
                        Description = reader.GetString(3),
                        BHK = reader.GetInt32(4),
                        LocationId = reader.GetInt32(5),
                        Location = reader.GetString(6),
                        ImagesBase64 = new List<string>()
                    };
                }
            }
        }

        // 🔥 Load images separately (PERFORMANCE WIN)
        LoadImages(properties);

        return properties.Values.ToList();
    }
private int GetTotalPropertyCount(PropertyFilterVM filter)
{
    using var con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;" +
                                      "Initial Catalog=db_ac36b8_ronakrealestate00;" +
                                      "User ID=db_ac36b8_ronakrealestate00_admin;" +
                                      "Password=Ronak0910#;" +
                                      "Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;");

    string query = @"
        SELECT COUNT(*)
        FROM Properties p
        JOIN LocationMaster lm ON p.Location = lm.Id
        WHERE p.IsActive = 1
          AND p.LookingFor = @Mode
          AND (@LocationId IS NULL OR lm.Id = @LocationId)
          AND (@BHK IS NULL OR p.BHK = @BHK)
    ";

    var cmd = new SqlCommand(query, con);

    cmd.Parameters.AddWithValue("@Mode", filter.Mode == "Buy" ? 2 :
        filter.Mode == "Rent" ? 1 : 3);
    cmd.Parameters.AddWithValue("@LocationId",
        (object?)filter.LocationId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@BHK",
        (object?)filter.BHK ?? DBNull.Value);

    con.Open();
    return (int)cmd.ExecuteScalar();
}

    private void LoadImages(Dictionary<int, PropertyViewModel> properties)
    {
        if (!properties.Any()) return;

        using (SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;" +
                                      "Initial Catalog=db_ac36b8_ronakrealestate00;" +
                                      "User ID=db_ac36b8_ronakrealestate00_admin;" +
                                      "Password=Ronak0910#;" +
                                      "Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
        {
            string query = $@"
                SELECT PropertyId, ImageBase64
                FROM PropertyImages
                WHERE PropertyId IN ({string.Join(",", properties.Keys)})
            ";

            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int propertyId = reader.GetInt32(0);
                    string image = reader.GetString(1);

                    if (properties.ContainsKey(propertyId))
                        properties[propertyId].ImagesBase64.Add(image);
                }
            }
        }
    }
    


        [HttpGet]
        public IActionResult Sale()
        {
            var LocationDetails = GetLocations();
            ViewBag.Locations = LocationDetails;
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
            return View("Sale");
        }

        [HttpPost]
        public IActionResult Sale(PropertyViewModel model)
        {
            // 1️⃣ Validate input
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill all required fields correctly.";
                return RedirectToAction("SellProperty");
            }

            // 2️⃣ Map location number to name
            string locationName = model.Location switch
            {
                "1" => "Borivali",
                "2" => "Kandivali",
                "3" => "Goregaon",
                "4" => "Malad",
                _ => "Unknown"
            };

           

            //  Prepare WhatsApp message
            string phoneNumber = "919594774795";

            string messageText =
                $@"Hello Ronak Estate 👋

                I would like to sell my property 🏠

                Property Details
                Title: {model.Title}
                Location: {locationName}
                BHK: {model.BHK}
                Expected Price: ₹{model.Price:N0}

                Please contact me for further discussion.
                Thank you 🙂";

            string message = Uri.EscapeDataString(messageText);

            // 5️⃣ Redirect to WhatsApp
            return Redirect($"https://wa.me/{phoneNumber}?text={message}");
        }

        [HttpGet]
public IActionResult SalePartial()
{
    var LocationDetails = GetLocations();
    ViewBag.Locations = LocationDetails;

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

    return PartialView("Sale");
}

    }
}

