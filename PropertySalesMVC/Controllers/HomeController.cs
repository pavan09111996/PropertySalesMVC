using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
