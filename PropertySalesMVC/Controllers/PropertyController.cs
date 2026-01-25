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
    PropertyViewModel model = new();

    using (SqlConnection con = new SqlConnection(
        "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;" +
        "User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;"))
    {
        con.Open();

        // PROPERTY
        SqlCommand cmd = new SqlCommand(@"
            SELECT p.Id, p.Title, ISNULL(lm.Location,''), p.Price, p.Description, p.BHK, p.VideoPath
            FROM Properties p
            LEFT JOIN LocationMaster lm ON p.Location = lm.Id
            WHERE p.Id = @Id AND p.IsActive = 1", con);

        cmd.Parameters.AddWithValue("@Id", id);

        using (var dr = cmd.ExecuteReader())
        {
            if (!dr.Read()) return NotFound();

            model.PropertyId = dr.GetInt32(0);
            model.Title = dr.GetString(1);
            model.Location = dr.GetString(2);
            model.Price = dr.GetDecimal(3);
            model.Description = dr.GetString(4);
            model.BHK = dr.GetInt32(5);
            model.VideoPath = dr.IsDBNull(6) ? null : dr.GetString(6);
        }

        // IMAGES
        SqlCommand imgCmd = new SqlCommand(
            "SELECT ImagePath FROM PropertyImages WHERE PropertyId = @Id", con);
        imgCmd.Parameters.AddWithValue("@Id", id);

        using (var r = imgCmd.ExecuteReader())
        {
            while (r.Read())
                model.Images.Add(r.GetString(0));
        }
    }

    return View(model);
}


[HttpGet]
[AdminAuthorize]
// on click of Edit
public IActionResult EditProperty(int id)
{
    EditPropertyViewModel model = new();

    using (SqlConnection con = new SqlConnection(
        "Data Source=SQL6031.site4now.net,1433;" +
        "Initial Catalog=db_ac36b8_ronakrealestate00;" +
        "User ID=db_ac36b8_ronakrealestate00_admin;" +
        "Password=Ronak0910#;" +
        "Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
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
                BHK,
                VideoPath
            FROM Properties
            WHERE Id = @Id";

        using (SqlCommand cmd = new SqlCommand(propertyQuery, con))
        {
            cmd.Parameters.AddWithValue("@Id", id);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                    return NotFound();

                model.PropertyId = id;
                model.Title = reader["Title"].ToString();
                model.LocationId = Convert.ToInt32(reader["Location"]);
                model.Price = Convert.ToDecimal(reader["Price"]);
                model.Description = reader["Description"].ToString();
                model.LookingFor = Convert.ToInt32(reader["LookingFor"]);
                model.BHK = Convert.ToInt32(reader["BHK"]);

                // ✅ EXISTING VIDEO
                if (!reader.IsDBNull(reader.GetOrdinal("VideoPath")))
                {
                    model.ExistingVideoPath = reader["VideoPath"].ToString();
                }
            }
        }

        /* =======================
           2️⃣ GET PROPERTY IMAGES
           (ImagePath preferred, Base64 fallback)
           ======================= */
        string imageQuery = @"
            SELECT ImageId, ImagePath, ImageBase64
            FROM PropertyImages
            WHERE PropertyId = @Id";

        using (SqlCommand imgCmd = new SqlCommand(imageQuery, con))
        {
            imgCmd.Parameters.AddWithValue("@Id", id);

            using (SqlDataReader imgReader = imgCmd.ExecuteReader())
            {
                while (imgReader.Read())
                {
                    PropertyImageViewModel imageVm = new()
                    {
                        ImageId = Convert.ToInt32(imgReader["ImageId"])
                    };

                    if (!imgReader.IsDBNull(imgReader.GetOrdinal("ImagePath")))
                    {
                        imageVm.ImagePath = imgReader["ImagePath"].ToString();
                    }
                    else if (!imgReader.IsDBNull(imgReader.GetOrdinal("ImageBase64")))
                    {
                        imageVm.ImageBase64 = imgReader["ImageBase64"].ToString();
                    }

                    model.ExistingImages.Add(imageVm);
                }
            }
        }

        /* =======================
           3️⃣ GET LOCATIONS (DROPDOWN)
           ======================= */
        List<LocationViewModel> locations = new();

        string locQuery = @"
            SELECT 
                Id,
                Location AS LocationName
            FROM LocationMaster
            WHERE IsActive = 1";

        using (SqlCommand locCmd = new SqlCommand(locQuery, con))
        using (SqlDataReader locReader = locCmd.ExecuteReader())
        {
            while (locReader.Read())
            {
                locations.Add(new LocationViewModel
                {
                    Id = Convert.ToInt32(locReader["Id"]),
                    LocationName = locReader["LocationName"].ToString()
                });
            }
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
[RequestSizeLimit(50 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
public async Task<IActionResult> EditProperty(EditPropertyViewModel model)
{
    model.RemoveImageIds ??= new List<int>();
    model.NewImages ??= new List<IFormFile>();

    if (!ModelState.IsValid)
    {
        model.ExistingImages = GetPropertyImages(model.PropertyId);
        return View(model);
    }

    string connectionString =
        "Data Source=SQL6031.site4now.net,1433;" +
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
            cmd.Parameters.AddWithValue("@Id", model.PropertyId);
            cmd.Parameters.AddWithValue("@Title", model.Title);
            cmd.Parameters.AddWithValue("@Location", model.LocationId);
            cmd.Parameters.AddWithValue("@Price", model.Price);
            cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
            cmd.Parameters.AddWithValue("@LookingFor", model.LookingFor);
            cmd.Parameters.AddWithValue("@BHK", model.BHK);

            await cmd.ExecuteNonQueryAsync();
        }

        /* ===============================
           2️⃣ DELETE MARKED IMAGES (DB + FILE)
        =============================== */
        if (model.RemoveImageIds.Any())
        {
            foreach (int imageId in model.RemoveImageIds.Distinct())
            {
                string getPathQuery = "SELECT ImagePath FROM PropertyImages WHERE ImageId=@ImageId";
                await using SqlCommand pathCmd = new SqlCommand(getPathQuery, con, tran);
                pathCmd.Parameters.AddWithValue("@ImageId", imageId);

                string? imagePath = pathCmd.ExecuteScalar() as string;

                if (!string.IsNullOrEmpty(imagePath))
                {
                    string fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        imagePath.TrimStart('/'));

                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                await using SqlCommand deleteCmd = new SqlCommand(
                    "DELETE FROM PropertyImages WHERE ImageId=@ImageId AND PropertyId=@PropertyId",
                    con, tran);

                deleteCmd.Parameters.AddWithValue("@ImageId", imageId);
                deleteCmd.Parameters.AddWithValue("@PropertyId", model.PropertyId);

                await deleteCmd.ExecuteNonQueryAsync();
            }
        }
/* ===============================
   REMOVE EXISTING VIDEO (IF CHECKED)
=============================== */
if (model.RemoveVideo)
{
    // 1️⃣ Get existing video path
    string getVideoQuery = "SELECT VideoPath FROM Properties WHERE Id = @Id";

    string? existingVideoPath = null;

    using (SqlCommand getCmd = new SqlCommand(getVideoQuery, con, tran))
    {
        getCmd.Parameters.AddWithValue("@Id", model.PropertyId);
        existingVideoPath = getCmd.ExecuteScalar() as string;
    }

    // 2️⃣ Delete file from disk
    if (!string.IsNullOrEmpty(existingVideoPath))
    {
        string fullVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            existingVideoPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
        );

        if (System.IO.File.Exists(fullVideoPath))
        {
            System.IO.File.Delete(fullVideoPath);
        }
    }

    // 3️⃣ Remove DB reference
    using (SqlCommand clearCmd = new SqlCommand(
        "UPDATE Properties SET VideoPath = NULL WHERE Id = @Id", con, tran))
    {
        clearCmd.Parameters.AddWithValue("@Id", model.PropertyId);
        await clearCmd.ExecuteNonQueryAsync();
    }
}
        /* ===============================
           3️⃣ ADD NEW IMAGES (FILE BASED)
        =============================== */
        if (model.NewImages.Any())
        {
            string imageDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "properties",
                model.PropertyId.ToString());

            Directory.CreateDirectory(imageDir);

            foreach (var img in model.NewImages)
            {
                if (img == null || img.Length == 0) continue;

                if (img.Length > 5 * 1024 * 1024)
                    throw new InvalidOperationException("Image exceeds 5MB.");

                string fileName = Guid.NewGuid() + Path.GetExtension(img.FileName);
                string fullPath = Path.Combine(imageDir, fileName);

                await using (FileStream fs = new FileStream(fullPath, FileMode.Create))
                {
                    await img.CopyToAsync(fs);
                }

                string imagePath = $"/uploads/properties/{model.PropertyId}/{fileName}";

                await using SqlCommand insertImgCmd = new SqlCommand(@"
                    INSERT INTO PropertyImages (PropertyId, ImagePath)
                    VALUES (@Pid, @Path)", con, tran);

                insertImgCmd.Parameters.AddWithValue("@Pid", model.PropertyId);
                insertImgCmd.Parameters.AddWithValue("@Path", imagePath);

                await insertImgCmd.ExecuteNonQueryAsync();
            }
        }

        /* ===============================
           4️⃣ HANDLE VIDEO (KEEP / REPLACE / DELETE)
        =============================== */
        string propertyRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "properties",
            model.PropertyId.ToString());

        string videoDir = Path.Combine(propertyRoot, "video");
        Directory.CreateDirectory(videoDir);

        // REMOVE VIDEO
        if (model.RemoveVideo)
        {
            if (!string.IsNullOrEmpty(model.ExistingVideoPath))
            {
                string oldVideo = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    model.ExistingVideoPath.TrimStart('/'));

                if (System.IO.File.Exists(oldVideo))
                    System.IO.File.Delete(oldVideo);
            }

            await using SqlCommand clearVideoCmd = new SqlCommand(
                "UPDATE Properties SET VideoPath = NULL WHERE Id = @Id",
                con, tran);

            clearVideoCmd.Parameters.AddWithValue("@Id", model.PropertyId);
            await clearVideoCmd.ExecuteNonQueryAsync();
        }

        // ADD / REPLACE VIDEO
        if (model.NewVideo != null && model.NewVideo.Length > 0)
        {
            if (model.NewVideo.Length > 20 * 1024 * 1024)
                throw new InvalidOperationException("Video exceeds 20MB.");

            if (!string.IsNullOrEmpty(model.ExistingVideoPath))
            {
                string oldVideo = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    model.ExistingVideoPath.TrimStart('/'));

                if (System.IO.File.Exists(oldVideo))
                    System.IO.File.Delete(oldVideo);
            }

            string videoName = "property-video" + Path.GetExtension(model.NewVideo.FileName);
            string videoFullPath = Path.Combine(videoDir, videoName);

            await using (FileStream fs = new FileStream(videoFullPath, FileMode.Create))
            {
                await model.NewVideo.CopyToAsync(fs);
            }

            string dbVideoPath = $"/uploads/properties/{model.PropertyId}/video/{videoName}";

            await using SqlCommand updateVideoCmd = new SqlCommand(
                "UPDATE Properties SET VideoPath = @Video WHERE Id = @Id",
                con, tran);

            updateVideoCmd.Parameters.AddWithValue("@Video", dbVideoPath);
            updateVideoCmd.Parameters.AddWithValue("@Id", model.PropertyId);

            await updateVideoCmd.ExecuteNonQueryAsync();
        }

        await tran.CommitAsync();

        TempData["PropertyUpdateMessage"] = "Property updated successfully.";
        return RedirectToAction("Dashboard", "Admin");
    }
    catch (Exception ex)
    {
        await tran.RollbackAsync();

        _logger.LogError(ex, "EditProperty failed for PropertyId {Id}", model.PropertyId);

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
                p.Price,
                p.Description,
                p.BHK,
                lm.Id AS LocationId,
                lm.Location,
                (
                    SELECT TOP 1 ImagePath
                    FROM PropertyImages
                    WHERE PropertyId = p.Id
                    ORDER BY ImageId
                ) AS ImagePath
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

        cmd.Parameters.AddWithValue("@Mode",
            filter.Mode == "Buy" ? 2 :
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
                properties.Add(new PropertyViewModel
                {
                    PropertyId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Price = reader.GetDecimal(2),
                    Description = reader.GetString(3),
                    BHK = reader.GetInt32(4),
                    LocationId = reader.GetInt32(5),
                    Location = reader.GetString(6),
                    ImagePath = reader.IsDBNull(7)
                        ? "/images/no-image.png"
                        : reader.GetString(7)
                });
            }
        }
    }

    return properties;
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

