using System.Data.SqlClient;

public class DbHelper
{
    private readonly string _connectionString;

    public DbHelper(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection");
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
