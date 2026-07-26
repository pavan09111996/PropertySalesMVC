using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public class EnquiryRepository : IEnquiryRepository
    {
        private readonly string _connectionString;

        public EnquiryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        private SqlConnection CreateConnection() => new(_connectionString);

        public async Task InsertEnquiryAsync(NewEnquiry enquiry)
        {
            await using var con = CreateConnection();
            const string query = @"
                INSERT INTO PropertyEnquiries
                    (EnquiryType, PropertyId, Name, Phone, Message, Title, BHK, Price, Description, CreatedOn)
                VALUES
                    (@EnquiryType, @PropertyId, @Name, @Phone, @Message, @Title, @BHK, @Price, @Description, GETDATE())";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@EnquiryType", enquiry.EnquiryType);
            cmd.Parameters.AddWithValue("@PropertyId", (object?)enquiry.PropertyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", (object?)enquiry.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", (object?)enquiry.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Message", (object?)enquiry.Message ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Title", (object?)enquiry.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BHK", (object?)enquiry.BHK ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Price", (object?)enquiry.Price ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)enquiry.Description ?? DBNull.Value);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Enquiry>> GetEnquiriesAsync(int page, int pageSize)
        {
            var enquiries = new List<Enquiry>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT
                    e.Id, e.EnquiryType, e.PropertyId, p.Title AS PropertyTitle,
                    e.Name, e.Phone, e.Message, e.Title, e.BHK, e.Price, e.Description, e.CreatedOn
                FROM PropertyEnquiries e
                LEFT JOIN Properties p ON e.PropertyId = p.Id
                ORDER BY e.CreatedOn DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                enquiries.Add(new Enquiry
                {
                    Id = reader.GetInt32(0),
                    EnquiryType = reader.GetString(1),
                    PropertyId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    PropertyTitle = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Name = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Phone = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Message = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Title = reader.IsDBNull(7) ? null : reader.GetString(7),
                    BHK = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    Price = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                    Description = reader.IsDBNull(10) ? null : reader.GetString(10),
                    CreatedOn = reader.GetDateTime(11)
                });
            }

            return enquiries;
        }

        public async Task<int> GetEnquiryCountAsync()
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand("SELECT COUNT(*) FROM PropertyEnquiries", con);

            await con.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
        }
    }
}
