using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace PropertySalesMVC.Helpers
{
    public class DbHelper
    {
        private readonly IConfiguration _config;

        public DbHelper(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(
                _config.GetConnectionString("DefaultConnection"));
        }
    }
}
