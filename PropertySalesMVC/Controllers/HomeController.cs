using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;
using PropertySalesMVC.Helpers;

namespace PropertySalesMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(int page = 1)
        {
            const int pageSize = 6; // properties per page

            var properties = new Dictionary<int, PropertyViewModel>();
            int totalRecords = 0;

            using (SqlConnection con = new SqlConnection(
                "Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"))
            {
                con.Open();

               
                using (SqlCommand countCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Properties WHERE IsActive = 1", con))
                {
                    totalRecords = (int)countCmd.ExecuteScalar();
                }

                
                List<int> propertyIds = new();

                using (SqlCommand idCmd = new SqlCommand(@"
            SELECT Id
            FROM Properties
            WHERE IsActive = 1
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", con))
                {
                    idCmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    idCmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using SqlDataReader idReader = idCmd.ExecuteReader();
                    while (idReader.Read())
                    {
                        propertyIds.Add(idReader.GetInt32(0));
                    }
                }

                if (propertyIds.Count == 0)
                {
                    return View(new PagedResult<PropertyViewModel>
                    {
                        Items = new List<PropertyViewModel>(),
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalRecords = totalRecords
                    });
                }

                
                string query = $@"
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
            WHERE p.Id IN ({string.Join(",", propertyIds)})
            ORDER BY p.Id DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int propertyId = reader.GetInt32(0);

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


            //AdminDetails fetch here for AdressBar

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

            //Get Session
            //HttpContext.Session.SetString(SessionKeys.AdminName, "Welcome Ronak");


            //return View(properties.Values.ToList());


            var model = new PagedResult<PropertyViewModel>
            {
                Items = properties.Values.ToList(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };

            ViewBag.PropertyCount = totalRecords;

            return View(model);
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



        [HttpGet]
        public IActionResult Contact()
        {
            ViewBag.Admin = GetActiveAdmin();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            AdminContactInfo admin = GetActiveAdmin();

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

        private AdminContactInfo GetActiveAdmin()
        {
            using SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;");
            con.Open();

            using SqlCommand cmd = new SqlCommand(@"
        SELECT TOP 1 AdminId, AdminName, Phone, WhatsApp, Email
        FROM AdminMaster
        WHERE IsActive = 1
        ORDER BY CreatedOn DESC", con);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new AdminContactInfo
                {
                    AdminId = (int)dr["AdminId"],
                    AdminName = dr["AdminName"].ToString(),
                    Phone = dr["Phone"].ToString(),
                    WhatsApp = dr["WhatsApp"].ToString(),
                    Email = dr["Email"]?.ToString()
                };
            }

            return null;
        }

    }
}
