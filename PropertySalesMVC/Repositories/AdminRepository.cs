using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        private SqlConnection CreateConnection() => new(_connectionString);

        public async Task<AdminDetails?> GetActiveAdminDetailsAsync()
        {
            await using var con = CreateConnection();
            await con.OpenAsync();

            const string query = "SELECT TOP 1 * FROM AdminDetails WHERE IsActive = 1";

            await using var cmd = new SqlCommand(query, con);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new AdminDetails
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

        public async Task<AdminContactInfo?> GetActiveAdminContactAsync()
        {
            await using var con = CreateConnection();
            await con.OpenAsync();

            await using var cmd = new SqlCommand(@"
                SELECT TOP 1 AdminId, AdminName, Phone, WhatsApp, Email
                FROM AdminMaster
                WHERE IsActive = 1
                ORDER BY CreatedOn DESC", con);

            await using var dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
                return null;

            return new AdminContactInfo
            {
                AdminId = (int)dr["AdminId"],
                AdminName = dr["AdminName"].ToString(),
                Phone = dr["Phone"].ToString(),
                WhatsApp = dr["WhatsApp"].ToString(),
                Email = dr["Email"]?.ToString()
            };
        }

        public async Task<AdminLoginRecord?> GetLoginRecordAsync(string adminId)
        {
            await using var con = CreateConnection();
            const string query = @"
                SELECT TOP 1 Id, PasswordHash, FailedAttempts, LockoutUntil, IsActive, Role
                FROM AdminLoginDetails
                WHERE AdminID = @AdminID";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@AdminID", adminId);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new AdminLoginRecord
            {
                Id = reader.GetInt32(0),
                PasswordHash = reader.GetString(1),
                FailedAttempts = reader.GetInt32(2),
                LockoutUntil = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                IsActive = reader.GetBoolean(4),
                Role = reader.GetString(5)
            };
        }

        public async Task RecordLoginSuccessAsync(int loginRecordId)
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "UPDATE AdminLoginDetails SET FailedAttempts = 0, LockoutUntil = NULL WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", loginRecordId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RecordLoginFailureAsync(int loginRecordId, int failedAttempts, DateTime? lockoutUntil)
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "UPDATE AdminLoginDetails SET FailedAttempts = @FailedAttempts, LockoutUntil = @LockoutUntil WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", loginRecordId);
            cmd.Parameters.AddWithValue("@FailedAttempts", failedAttempts);
            cmd.Parameters.AddWithValue("@LockoutUntil", (object?)lockoutUntil ?? DBNull.Value);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAdminDetailsAsync(AdminDetails details)
        {
            await using var con = CreateConnection();
            await con.OpenAsync();

            await using (var updateCmd = new SqlCommand(@"
                UPDATE AdminDetails
                SET CompanyName = @CompanyName,
                    OwnerName = @OwnerName,
                    Designation = @Designation,
                    HeadOfficeTitle = @HeadOfficeTitle,
                    HeadOfficeAddress = @HeadOfficeAddress,
                    BranchOfficeTitle = @BranchOfficeTitle,
                    BranchOfficeAddress = @BranchOfficeAddress,
                    InstagramUrl = @InstagramUrl,
                    FacebookUrl = @FacebookUrl
                WHERE IsActive = 1", con))
            {
                AddAdminDetailsParameters(updateCmd, details);
                int rows = await updateCmd.ExecuteNonQueryAsync();
                if (rows > 0) return;
            }

            await using var insertCmd = new SqlCommand(@"
                INSERT INTO AdminDetails
                    (CompanyName, OwnerName, Designation, HeadOfficeTitle, HeadOfficeAddress,
                     BranchOfficeTitle, BranchOfficeAddress, InstagramUrl, FacebookUrl, IsActive, CreatedOn)
                VALUES
                    (@CompanyName, @OwnerName, @Designation, @HeadOfficeTitle, @HeadOfficeAddress,
                     @BranchOfficeTitle, @BranchOfficeAddress, @InstagramUrl, @FacebookUrl, 1, GETDATE())", con);
            AddAdminDetailsParameters(insertCmd, details);
            await insertCmd.ExecuteNonQueryAsync();
        }

        private static void AddAdminDetailsParameters(SqlCommand cmd, AdminDetails details)
        {
            cmd.Parameters.AddWithValue("@CompanyName", (object?)details.CompanyName ?? "");
            cmd.Parameters.AddWithValue("@OwnerName", (object?)details.OwnerName ?? "");
            cmd.Parameters.AddWithValue("@Designation", (object?)details.Designation ?? "");
            cmd.Parameters.AddWithValue("@HeadOfficeTitle", (object?)details.HeadOfficeTitle ?? "");
            cmd.Parameters.AddWithValue("@HeadOfficeAddress", (object?)details.HeadOfficeAddress ?? "");
            cmd.Parameters.AddWithValue("@BranchOfficeTitle", (object?)details.BranchOfficeTitle ?? "");
            cmd.Parameters.AddWithValue("@BranchOfficeAddress", (object?)details.BranchOfficeAddress ?? "");
            cmd.Parameters.AddWithValue("@InstagramUrl", (object?)details.InstagramUrl ?? "");
            cmd.Parameters.AddWithValue("@FacebookUrl", (object?)details.FacebookUrl ?? "");
        }

        public async Task UpdateAdminMasterAsync(AdminContactInfo contact)
        {
            await using var con = CreateConnection();
            await con.OpenAsync();

            await using (var updateCmd = new SqlCommand(@"
                UPDATE AdminMaster
                SET AdminName = @AdminName,
                    Phone = @Phone,
                    WhatsApp = @WhatsApp,
                    Email = @Email
                WHERE IsActive = 1", con))
            {
                AddAdminMasterParameters(updateCmd, contact);
                int rows = await updateCmd.ExecuteNonQueryAsync();
                if (rows > 0) return;
            }

            await using var insertCmd = new SqlCommand(@"
                INSERT INTO AdminMaster (AdminName, Phone, WhatsApp, Email, IsActive, CreatedOn)
                VALUES (@AdminName, @Phone, @WhatsApp, @Email, 1, GETDATE())", con);
            AddAdminMasterParameters(insertCmd, contact);
            await insertCmd.ExecuteNonQueryAsync();
        }

        private static void AddAdminMasterParameters(SqlCommand cmd, AdminContactInfo contact)
        {
            cmd.Parameters.AddWithValue("@AdminName", (object?)contact.AdminName ?? "");
            cmd.Parameters.AddWithValue("@Phone", (object?)contact.Phone ?? "");
            cmd.Parameters.AddWithValue("@WhatsApp", (object?)contact.WhatsApp ?? "");
            cmd.Parameters.AddWithValue("@Email", (object?)contact.Email ?? "");
        }
    }
}
