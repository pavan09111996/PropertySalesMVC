using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly string _connectionString;

        public PropertyRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        private SqlConnection CreateConnection() => new(_connectionString);

        public async Task<(List<PropertyViewModel> Items, int TotalCount)> GetActivePropertiesForDashboardAsync(AdminPropertyFilter filter)
        {
            var properties = new List<PropertyViewModel>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT
                    p.Id,
                    p.Title,
                    ISNULL(lm.Location,'') AS Location,
                    p.Price,
                    p.Description,
                    img.ImagePath,
                    p.LookingFor
                FROM Properties p
                LEFT JOIN LocationMaster lm ON p.Location = lm.Id
                OUTER APPLY (
                    SELECT TOP 1 ImagePath
                    FROM PropertyImages
                    WHERE PropertyId = p.Id
                    ORDER BY p.Id
                ) img
                WHERE p.IsActive = 1
                  AND (@Search IS NULL OR p.Title LIKE '%' + @Search + '%')
                  AND (@LookingFor IS NULL OR p.LookingFor = @LookingFor)
                ORDER BY p.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LookingFor", (object?)filter.LookingFor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Offset", (filter.Page - 1) * filter.PageSize);
            cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

            await con.OpenAsync();

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var images = new List<string>();

                    if (!reader.IsDBNull(5))
                    {
                        images.Add(reader.GetString(5)); // ImagePath
                    }

                    properties.Add(new PropertyViewModel
                    {
                        PropertyId = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Location = reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Description = reader.GetString(4),
                        Images = images,
                        LookingFor = reader.GetInt32(6)
                    });
                }
            }

            const string countQuery = @"
                SELECT COUNT(*)
                FROM Properties p
                WHERE p.IsActive = 1
                  AND (@Search IS NULL OR p.Title LIKE '%' + @Search + '%')
                  AND (@LookingFor IS NULL OR p.LookingFor = @LookingFor)";

            await using var countCmd = new SqlCommand(countQuery, con);
            countCmd.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            countCmd.Parameters.AddWithValue("@LookingFor", (object?)filter.LookingFor ?? DBNull.Value);
            int totalCount = (int)(await countCmd.ExecuteScalarAsync())!;

            return (properties, totalCount);
        }

        public async Task<int> InsertPropertyAsync(AddPropertyViewModel model)
        {
            await using var con = CreateConnection();
            const string query = @"
                INSERT INTO Properties
                (Title, Location, Price, Description, LookingFor, BHK, IsFeatured)
                OUTPUT INSERTED.Id
                VALUES
                (@Title, @Location, @Price, @Description, @LookingFor, @BHK, @IsFeatured)";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Title", model.Title);
            cmd.Parameters.AddWithValue("@Location", model.Location);
            cmd.Parameters.AddWithValue("@Price", model.Price);
            cmd.Parameters.AddWithValue("@Description", model.Description);
            cmd.Parameters.AddWithValue("@LookingFor", model.LookingFor);
            cmd.Parameters.AddWithValue("@BHK", model.BHK);
            cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);

            await con.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task InsertPropertyImageAsync(int propertyId, string imagePath)
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "INSERT INTO PropertyImages (PropertyId, ImagePath) VALUES (@Pid, @Path)", con);
            cmd.Parameters.AddWithValue("@Pid", propertyId);
            cmd.Parameters.AddWithValue("@Path", imagePath);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePropertyVideoPathAsync(int propertyId, string videoPath)
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "UPDATE Properties SET VideoPath=@Video WHERE Id=@Id", con);
            cmd.Parameters.AddWithValue("@Video", videoPath);
            cmd.Parameters.AddWithValue("@Id", propertyId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<(List<string> ImagePaths, string? VideoPath)> GetPropertyFilesForDeleteAsync(int propertyId)
        {
            var imagePaths = new List<string>();
            string? videoPath = null;

            await using var con = CreateConnection();
            await con.OpenAsync();

            await using (var imgCmd = new SqlCommand(
                "SELECT ImagePath FROM PropertyImages WHERE PropertyId = @Id", con))
            {
                imgCmd.Parameters.AddWithValue("@Id", propertyId);
                await using var reader = await imgCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                        imagePaths.Add(reader.GetString(0));
                }
            }

            await using (var vidCmd = new SqlCommand(
                "SELECT VideoPath FROM Properties WHERE Id = @Id", con))
            {
                vidCmd.Parameters.AddWithValue("@Id", propertyId);
                videoPath = (await vidCmd.ExecuteScalarAsync()) as string;
            }

            return (imagePaths, videoPath);
        }

        public async Task DeletePropertyCascadeAsync(int propertyId)
        {
            await using var con = CreateConnection();
            await con.OpenAsync();
            await using var tran = (SqlTransaction)await con.BeginTransactionAsync();

            try
            {
                await using (var delImagesCmd = new SqlCommand(
                    "DELETE FROM PropertyImages WHERE PropertyId = @Id", con, tran))
                {
                    delImagesCmd.Parameters.AddWithValue("@Id", propertyId);
                    await delImagesCmd.ExecuteNonQueryAsync();
                }

                await using (var delPropertyCmd = new SqlCommand(
                    "DELETE FROM Properties WHERE Id = @Id", con, tran))
                {
                    delPropertyCmd.Parameters.AddWithValue("@Id", propertyId);
                    await delPropertyCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<PropertyViewModel?> GetPropertyDetailCoreAsync(int id)
        {
            await using var con = CreateConnection();
            const string query = @"
                SELECT p.Id, p.Title, ISNULL(lm.Location,''), p.Price, p.Description, p.BHK, p.VideoPath
                FROM Properties p
                LEFT JOIN LocationMaster lm ON p.Location = lm.Id
                WHERE p.Id = @Id AND p.IsActive = 1";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Id", id);

            await con.OpenAsync();
            await using var dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
                return null;

            return new PropertyViewModel
            {
                PropertyId = dr.GetInt32(0),
                Title = dr.GetString(1),
                Location = dr.GetString(2),
                Price = dr.GetDecimal(3),
                Description = dr.GetString(4),
                BHK = dr.GetInt32(5),
                VideoPath = dr.IsDBNull(6) ? null : dr.GetString(6)
            };
        }

        public async Task<List<string>> GetPropertyImagePathsListAsync(int id)
        {
            var images = new List<string>();

            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "SELECT ImagePath FROM PropertyImages WHERE PropertyId = @Id", con);
            cmd.Parameters.AddWithValue("@Id", id);

            await con.OpenAsync();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                images.Add(r.GetString(0));

            return images;
        }

        public async Task<EditPropertyViewModel?> GetPropertyForEditAsync(int id)
        {
            await using var con = CreateConnection();
            const string propertyQuery = @"
                SELECT
                    Id,
                    Title,
                    Location,
                    Price,
                    Description,
                    LookingFor,
                    BHK,
                    VideoPath,
                    IsFeatured
                FROM Properties
                WHERE Id = @Id";

            await using var cmd = new SqlCommand(propertyQuery, con);
            cmd.Parameters.AddWithValue("@Id", id);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var model = new EditPropertyViewModel
            {
                PropertyId = id,
                Title = reader["Title"].ToString(),
                LocationId = Convert.ToInt32(reader["Location"]),
                Price = Convert.ToDecimal(reader["Price"]),
                Description = reader["Description"].ToString(),
                LookingFor = Convert.ToInt32(reader["LookingFor"]),
                BHK = Convert.ToInt32(reader["BHK"]),
                IsFeatured = Convert.ToBoolean(reader["IsFeatured"])
            };

            if (!reader.IsDBNull(reader.GetOrdinal("VideoPath")))
            {
                model.ExistingVideoPath = reader["VideoPath"].ToString();
            }

            return model;
        }

        public async Task<List<PropertyImageViewModel>> GetPropertyImagesForEditAsync(int id)
        {
            var images = new List<PropertyImageViewModel>();

            await using var con = CreateConnection();
            const string imageQuery = @"
                SELECT ImageId, ImagePath
                FROM PropertyImages
                WHERE PropertyId = @Id";

            await using var imgCmd = new SqlCommand(imageQuery, con);
            imgCmd.Parameters.AddWithValue("@Id", id);

            await con.OpenAsync();
            await using var imgReader = await imgCmd.ExecuteReaderAsync();
            while (await imgReader.ReadAsync())
            {
                images.Add(new PropertyImageViewModel
                {
                    ImageId = Convert.ToInt32(imgReader["ImageId"]),
                    ImagePath = imgReader["ImagePath"].ToString()
                });
            }

            return images;
        }

        public async Task<List<PropertyImageViewModel>> GetPropertyImagesSimpleAsync(int propertyId)
        {
            var images = new List<PropertyImageViewModel>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT ImageId, ImagePath
                FROM PropertyImages
                WHERE PropertyId = @PropertyId";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@PropertyId", propertyId);

            await con.OpenAsync();
            await using var dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync())
            {
                images.Add(new PropertyImageViewModel
                {
                    ImageId = (int)dr["ImageId"],
                    ImagePath = dr["ImagePath"].ToString()
                });
            }

            return images;
        }

        public async Task<string?> GetImagePathByIdAsync(int imageId)
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "SELECT ImagePath FROM PropertyImages WHERE ImageId=@ImageId", con);
            cmd.Parameters.AddWithValue("@ImageId", imageId);

            await con.OpenAsync();
            return (await cmd.ExecuteScalarAsync()) as string;
        }

        public async Task UpdatePropertyWithChangesAsync(
            EditPropertyViewModel model,
            List<int> imageIdsToDelete,
            List<string> newImageWebPaths,
            bool clearVideo,
            string? newVideoWebPath)
        {
            await using var con = CreateConnection();
            await con.OpenAsync();
            await using var tran = (SqlTransaction)await con.BeginTransactionAsync();

            try
            {
                await using (var cmd = new SqlCommand(@"
                    UPDATE Properties
                    SET Title = @Title,
                        Location = @Location,
                        Price = @Price,
                        Description = @Description,
                        LookingFor = @LookingFor,
                        BHK = @BHK,
                        IsFeatured = @IsFeatured
                    WHERE Id = @Id", con, tran))
                {
                    cmd.Parameters.AddWithValue("@Id", model.PropertyId);
                    cmd.Parameters.AddWithValue("@Title", model.Title);
                    cmd.Parameters.AddWithValue("@Location", model.LocationId);
                    cmd.Parameters.AddWithValue("@Price", model.Price);
                    cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@LookingFor", model.LookingFor);
                    cmd.Parameters.AddWithValue("@BHK", model.BHK);
                    cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);

                    await cmd.ExecuteNonQueryAsync();
                }

                foreach (var imageId in imageIdsToDelete.Distinct())
                {
                    await using var deleteCmd = new SqlCommand(
                        "DELETE FROM PropertyImages WHERE ImageId=@ImageId AND PropertyId=@PropertyId",
                        con, tran);

                    deleteCmd.Parameters.AddWithValue("@ImageId", imageId);
                    deleteCmd.Parameters.AddWithValue("@PropertyId", model.PropertyId);

                    await deleteCmd.ExecuteNonQueryAsync();
                }

                if (clearVideo)
                {
                    await using var clearCmd = new SqlCommand(
                        "UPDATE Properties SET VideoPath = NULL WHERE Id = @Id", con, tran);
                    clearCmd.Parameters.AddWithValue("@Id", model.PropertyId);
                    await clearCmd.ExecuteNonQueryAsync();
                }

                foreach (var imagePath in newImageWebPaths)
                {
                    await using var insertImgCmd = new SqlCommand(@"
                        INSERT INTO PropertyImages (PropertyId, ImagePath)
                        VALUES (@Pid, @Path)", con, tran);

                    insertImgCmd.Parameters.AddWithValue("@Pid", model.PropertyId);
                    insertImgCmd.Parameters.AddWithValue("@Path", imagePath);

                    await insertImgCmd.ExecuteNonQueryAsync();
                }

                if (newVideoWebPath != null)
                {
                    await using var updateVideoCmd = new SqlCommand(
                        "UPDATE Properties SET VideoPath = @Video WHERE Id = @Id",
                        con, tran);

                    updateVideoCmd.Parameters.AddWithValue("@Video", newVideoWebPath);
                    updateVideoCmd.Parameters.AddWithValue("@Id", model.PropertyId);

                    await updateVideoCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Property>> GetAllPropertiesAsync()
        {
            var list = new List<Property>();

            await using var con = CreateConnection();
            await using var cmd = new SqlCommand("SELECT * FROM Properties", con);

            await con.OpenAsync();
            await using var dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync())
            {
                list.Add(new Property
                {
                    Title = dr["Title"].ToString(),
                    Location = dr["Location"].ToString(),
                    Price = (decimal)dr["Price"],
                    Description = dr["Description"]?.ToString()
                });
            }

            return list;
        }

        public async Task<List<PropertyViewModel>> GetFilteredPropertiesAsync(PropertyFilterVM filter, int modeCode)
        {
            var properties = new List<PropertyViewModel>();

            await using var con = CreateConnection();
            const string query = @"
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
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            await using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Mode", modeCode);
            cmd.Parameters.AddWithValue("@LocationId", (object?)filter.LocationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BHK", (object?)filter.BHK ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Offset", (filter.Page - 1) * filter.PageSize);
            cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
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
                    ImagePath = reader.IsDBNull(7) ? "/images/no-image.png" : reader.GetString(7)
                });
            }

            return properties;
        }

        public async Task<int> GetTotalPropertyCountAsync(PropertyFilterVM filter, int modeCode)
        {
            await using var con = CreateConnection();
            const string query = @"
                SELECT COUNT(*)
                FROM Properties p
                JOIN LocationMaster lm ON p.Location = lm.Id
                WHERE p.IsActive = 1
                  AND p.LookingFor = @Mode
                  AND (@LocationId IS NULL OR lm.Id = @LocationId)
                  AND (@BHK IS NULL OR p.BHK = @BHK)";

            await using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Mode", modeCode);
            cmd.Parameters.AddWithValue("@LocationId", (object?)filter.LocationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BHK", (object?)filter.BHK ?? DBNull.Value);

            await con.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<List<PropertyViewModel>> GetFeaturedPropertiesAsync()
        {
            var properties = new List<PropertyViewModel>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT TOP 12
                    p.Id,
                    p.Title,
                    p.Price,
                    p.Description,
                    p.BHK,
                    p.LookingFor,
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
                  AND p.IsFeatured = 1
                ORDER BY p.CreatedDate DESC";

            await using var cmd = new SqlCommand(query, con);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                properties.Add(new PropertyViewModel
                {
                    PropertyId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Price = reader.GetDecimal(2),
                    Description = reader.GetString(3),
                    BHK = reader.GetInt32(4),
                    LookingFor = reader.GetInt32(5),
                    LocationId = reader.GetInt32(6),
                    Location = reader.GetString(7),
                    ImagePath = reader.IsDBNull(8) ? "/images/no-image.png" : reader.GetString(8),
                    IsFeatured = true
                });
            }

            return properties;
        }
    }
}
