using Microsoft.Data.SqlClient;
using PropertySalesMVC.Models;

namespace PropertySalesMVC.Repositories
{
    public class ListingViewStatsRepository : IListingViewStatsRepository
    {
        private readonly string _connectionString;

        public ListingViewStatsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        private SqlConnection CreateConnection() => new(_connectionString);

        public async Task RecordViewAsync(string mode, int? locationId, int? bhk)
        {
            await using var con = CreateConnection();
            await con.OpenAsync();

            const string updateQuery = @"
                UPDATE ListingViewStats SET ViewCount = ViewCount + 1
                WHERE ViewDate = CAST(GETDATE() AS DATE)
                  AND Mode = @Mode
                  AND ((LocationId IS NULL AND @LocationId IS NULL) OR LocationId = @LocationId)
                  AND ((BHK IS NULL AND @BHK IS NULL) OR BHK = @BHK)";

            bool updated;
            await using (var updateCmd = new SqlCommand(updateQuery, con))
            {
                updateCmd.Parameters.AddWithValue("@Mode", mode);
                updateCmd.Parameters.AddWithValue("@LocationId", (object?)locationId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@BHK", (object?)bhk ?? DBNull.Value);

                updated = await updateCmd.ExecuteNonQueryAsync() > 0;
            }

            if (!updated)
            {
                const string insertQuery = @"
                    INSERT INTO ListingViewStats (ViewDate, Mode, LocationId, BHK, ViewCount)
                    VALUES (CAST(GETDATE() AS DATE), @Mode, @LocationId, @BHK, 1)";

                await using var insertCmd = new SqlCommand(insertQuery, con);
                insertCmd.Parameters.AddWithValue("@Mode", mode);
                insertCmd.Parameters.AddWithValue("@LocationId", (object?)locationId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@BHK", (object?)bhk ?? DBNull.Value);
                await insertCmd.ExecuteNonQueryAsync();
            }

            // Safety-net purge, not a real storage-pressure fix — this table's
            // growth is capped by (date x mode x location x BHK) combinations,
            // not by traffic, so even years of data stay tiny against the DB's
            // 1000MB budget. 2 years is generous on purpose: unlike ErrorLogs,
            // historical trend data here is worth keeping for season-over-season
            // comparison. Swallowed on failure (same reasoning as
            // Logging/DatabaseLogger's ErrorLogs purge) so a purge hiccup never
            // surfaces as a false "failed to record" error for a write that
            // actually succeeded.
            try
            {
                await using var purgeCmd = new SqlCommand(
                    "DELETE FROM ListingViewStats WHERE ViewDate < DATEADD(day, -730, GETDATE())", con);
                await purgeCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Nowhere useful to report this from — the view was already recorded above.
            }
        }

        public async Task<List<LocationViewStat>> GetTopLocationsAsync(DateTime since)
        {
            var stats = new List<LocationViewStat>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT
                    COALESCE(lm.Location, 'All Locations') AS LocationName,
                    SUM(CASE WHEN s.Mode = 'Buy' THEN s.ViewCount ELSE 0 END) AS BuyViews,
                    SUM(CASE WHEN s.Mode = 'Rent' THEN s.ViewCount ELSE 0 END) AS RentViews,
                    SUM(s.ViewCount) AS TotalViews
                FROM ListingViewStats s
                LEFT JOIN LocationMaster lm ON s.LocationId = lm.Id
                WHERE s.ViewDate >= @Since
                GROUP BY lm.Location
                ORDER BY TotalViews DESC";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Since", since.Date);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Add(new LocationViewStat
                {
                    LocationName = reader.GetString(0),
                    BuyViews = reader.GetInt32(1),
                    RentViews = reader.GetInt32(2),
                    TotalViews = reader.GetInt32(3)
                });
            }

            return stats;
        }

        public async Task<List<BhkViewStat>> GetTopBhkAsync(DateTime since)
        {
            var stats = new List<BhkViewStat>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT
                    s.BHK,
                    SUM(CASE WHEN s.Mode = 'Buy' THEN s.ViewCount ELSE 0 END) AS BuyViews,
                    SUM(CASE WHEN s.Mode = 'Rent' THEN s.ViewCount ELSE 0 END) AS RentViews,
                    SUM(s.ViewCount) AS TotalViews
                FROM ListingViewStats s
                WHERE s.ViewDate >= @Since
                GROUP BY s.BHK
                ORDER BY TotalViews DESC";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Since", since.Date);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Add(new BhkViewStat
                {
                    BHK = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    BuyViews = reader.GetInt32(1),
                    RentViews = reader.GetInt32(2),
                    TotalViews = reader.GetInt32(3)
                });
            }

            return stats;
        }

        public async Task<List<DailyViewStat>> GetDailyTrendAsync(DateTime since)
        {
            var stats = new List<DailyViewStat>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT s.ViewDate, SUM(s.ViewCount) AS TotalViews
                FROM ListingViewStats s
                WHERE s.ViewDate >= @Since
                GROUP BY s.ViewDate
                ORDER BY s.ViewDate ASC";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Since", since.Date);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Add(new DailyViewStat
                {
                    ViewDate = reader.GetDateTime(0),
                    TotalViews = reader.GetInt32(1)
                });
            }

            return stats;
        }

        public async Task<List<LocationBhkCell>> GetLocationBhkBreakdownAsync(DateTime since)
        {
            var cells = new List<LocationBhkCell>();

            await using var con = CreateConnection();
            const string query = @"
                SELECT
                    COALESCE(lm.Location, 'All Locations') AS LocationName,
                    s.BHK,
                    s.Mode,
                    SUM(s.ViewCount) AS ViewCount
                FROM ListingViewStats s
                LEFT JOIN LocationMaster lm ON s.LocationId = lm.Id
                WHERE s.ViewDate >= @Since
                GROUP BY lm.Location, s.BHK, s.Mode";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Since", since.Date);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cells.Add(new LocationBhkCell
                {
                    LocationName = reader.GetString(0),
                    BHK = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Mode = reader.GetString(2),
                    ViewCount = reader.GetInt32(3)
                });
            }

            return cells;
        }

        public async Task<(int TotalViews, int BuyViews, int RentViews)> GetTotalsAsync(DateTime since)
        {
            await using var con = CreateConnection();
            const string query = @"
                SELECT
                    SUM(s.ViewCount) AS TotalViews,
                    SUM(CASE WHEN s.Mode = 'Buy' THEN s.ViewCount ELSE 0 END) AS BuyViews,
                    SUM(CASE WHEN s.Mode = 'Rent' THEN s.ViewCount ELSE 0 END) AS RentViews
                FROM ListingViewStats s
                WHERE s.ViewDate >= @Since";

            await using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Since", since.Date);

            await con.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
            }

            return (0, 0, 0);
        }
    }
}
