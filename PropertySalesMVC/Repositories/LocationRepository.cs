using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly string _connectionString;

        public LocationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        private SqlConnection CreateConnection() => new(_connectionString);

        public async Task<List<LocationOption>> GetActiveLocationsAsync()
        {
            var locations = new List<LocationOption>();

            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "SELECT Id, Location FROM LocationMaster WHERE IsActive = 1 ORDER BY SortOrder", con);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                locations.Add(new LocationOption
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Location = reader["Location"].ToString()
                });
            }

            return locations;
        }

        public async Task<List<LocationOption>> GetAllLocationsOrderedAsync()
        {
            var locations = new List<LocationOption>();

            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "SELECT Id, Location FROM LocationMaster ORDER BY SortOrder", con);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                locations.Add(new LocationOption
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Location = reader["Location"].ToString()
                });
            }

            return locations;
        }

        public async Task<List<LocationOption>> GetAllLocationsForAdminAsync()
        {
            var locations = new List<LocationOption>();

            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "SELECT Id, Location, IsActive FROM LocationMaster ORDER BY SortOrder", con);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                locations.Add(new LocationOption
                {
                    Id = reader.GetInt32(0),
                    Location = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }

            return locations;
        }

        public async Task<int> CreateLocationAsync(string name)
        {
            await using var con = CreateConnection();
            // New locations default to the end of the display order (not 0,
            // which would jump them ahead of Borivali/Kandivali/Malad/Goregaon).
            await using var cmd = new SqlCommand(
                @"INSERT INTO LocationMaster (Location, IsActive, SortOrder)
                  OUTPUT INSERTED.Id
                  VALUES (@Location, 1, (SELECT ISNULL(MAX(SortOrder), 0) + 1 FROM LocationMaster))", con);
            cmd.Parameters.AddWithValue("@Location", name);

            await con.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdateLocationAsync(int id, string name, bool isActive)
        {
            await using var con = CreateConnection();
            await using var cmd = new SqlCommand(
                "UPDATE LocationMaster SET Location = @Location, IsActive = @IsActive WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Location", name);
            cmd.Parameters.AddWithValue("@IsActive", isActive);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
