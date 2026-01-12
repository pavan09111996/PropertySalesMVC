using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Controllers
{
    public class PropertyController : Controller
    {
        private readonly DbHelper _db;

        public PropertyController(DbHelper db)
        {
            _db = db;
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
    }
}

