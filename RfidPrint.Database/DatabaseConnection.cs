using Microsoft.Extensions.Configuration;
using Npgsql;

namespace RfidPrint.Database
{
    public class DatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("RfidDatabase")
                ?? throw new InvalidOperationException("Connection string 'RfidDatabase' not found.");
        }

        public NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);
    }
}