using Microsoft.AspNetCore.Mvc;
using PropertySalesMVC.Models;
using System.Data;
using System.Data.SqlClient;

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProperty(EditPropertyViewModel model)
        {
            // Ensure lists are never null
            model.RemoveImageIds ??= new List<int>();
            model.NewImages ??= new List<IFormFile>();

            if (!ModelState.IsValid)
            {
                // Rehydrate images for UI (POST does not send them)
                model.ExistingImages = GetPropertyImages(model.PropertyId);
                return View(model);
            }

            using SqlConnection con = new SqlConnection("Data Source=SQL6031.site4now.net,1433;Initial Catalog=db_ac36b8_ronakrealestate00;User ID=db_ac36b8_ronakrealestate00_admin;Password=Ronak0910#;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;");
            con.Open();

            using SqlTransaction tran = con.BeginTransaction();

            try
            {
                /* =====================================
                   1️⃣ UPDATE PROPERTY CORE DATA
                   (Always safe & idempotent)
                ===================================== */
                using (SqlCommand cmd = new SqlCommand(@"
            UPDATE Properties
            SET Title = @Title,
                Location = @Location,
                Price = @Price,
                Description = @Description
            WHERE Id = @Id", con, tran))
                {
                    cmd.Parameters.AddWithValue("@Id", model.PropertyId);
                    cmd.Parameters.AddWithValue("@Title", model.Title);
                    cmd.Parameters.AddWithValue("@Location", model.Location);
                    cmd.Parameters.AddWithValue("@Price", model.Price);
                    cmd.Parameters.AddWithValue("@Description", model.Description);
                    cmd.ExecuteNonQuery();
                }

                /* =====================================
                   2️⃣ DELETE ONLY IMAGES MARKED ❌
                   (NO delete if list empty)
                ===================================== */
                if (model.RemoveImageIds.Count > 0)
                {
                    using SqlCommand deleteCmd = new SqlCommand(@"
                DELETE FROM PropertyImages
                WHERE ImageId = @ImageId
                  AND PropertyId = @PropertyId", con, tran);

                    deleteCmd.Parameters.Add("@ImageId", SqlDbType.Int);
                    deleteCmd.Parameters.Add("@PropertyId", SqlDbType.Int)
                             .Value = model.PropertyId;

                    foreach (int imageId in model.RemoveImageIds.Distinct())
                    {
                        deleteCmd.Parameters["@ImageId"].Value = imageId;
                        deleteCmd.ExecuteNonQuery();
                    }
                }

                /* =====================================
                   3️⃣ INSERT ONLY NEW IMAGES
                   (NO insert if none selected)
                ===================================== */
                if (model.NewImages.Count > 0)
                {
                    using SqlCommand insertCmd = new SqlCommand(@"
                INSERT INTO PropertyImages (PropertyId, ImageBase64)
                VALUES (@PropertyId, @ImageBase64)", con, tran);

                    insertCmd.Parameters.Add("@PropertyId", SqlDbType.Int)
                             .Value = model.PropertyId;
                    insertCmd.Parameters.Add("@ImageBase64", SqlDbType.NVarChar);

                    foreach (var file in model.NewImages)
                    {
                        if (file == null || file.Length == 0)
                            continue;

                        using MemoryStream ms = new MemoryStream();
                        file.CopyTo(ms);

                        insertCmd.Parameters["@ImageBase64"].Value =
                            Convert.ToBase64String(ms.ToArray());

                        insertCmd.ExecuteNonQuery();
                    }
                }

                /* =====================================
                   4️⃣ COMMIT — ONLY IF ALL SUCCEED
                ===================================== */
                tran.Commit();

                TempData["SuccessMessage"] = "Property updated successfully.";
                return RedirectToAction("Admin");
            }
            catch
            {
                tran.Rollback();

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

