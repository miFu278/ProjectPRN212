using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using webAPI.Models;

namespace webAPI.Data
{
    public class VehicleRepository
    {
        private readonly string _connectionString;

        // Lấy conn string từ appsettings thông qua IConfiguration
        public VehicleRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("BatterySwapDb")
                               ?? throw new InvalidOperationException("Missing connection string 'BatterySwapDb'");
        }

        /// <summary>
        /// Kiểm tra xem vehicleId có thuộc về userId hay không.
        /// </summary>
        public async Task<bool> UserOwnsVehicleAsync(int userId, int vehicleId)
        {
            const string sql = "SELECT 1 FROM Vehicle WHERE Vehicle_ID = @Vehicle_ID AND User_ID = @User_ID";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Vehicle_ID", vehicleId);
            cmd.Parameters.AddWithValue("@User_ID", userId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }

        /// <summary>
        /// Lấy Battery_Type từ Vehicle_ID (join Vehicle với Vehicle_Model).
        /// </summary>
        public async Task<string?> GetBatteryTypeByVehicleIdAsync(int vehicleId)
        {
            const string sql =
                "SELECT m.Battery_Type " +
                "FROM Vehicle v JOIN Vehicle_Model m ON v.Model_ID = m.Model_ID " +
                "WHERE v.Vehicle_ID = @Vehicle_ID";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Vehicle_ID", vehicleId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value
                ? Convert.ToString(result)
                : null;
        }

        /// <summary>
        /// Lấy đầy đủ thông tin Vehicle + Model_Name, Brand, Battery_Type.
        /// </summary>
        public async Task<Vehicle?> GetVehicleByIdAsync(int vehicleId)
        {
            const string sql =
                "SELECT v.Vehicle_ID, v.User_ID, v.Model_ID, v.Vin, v.License_Plate, " +
                "       m.Model_Name, m.Brand, m.Battery_Type " +
                "FROM Vehicle v " +
                "JOIN Vehicle_Model m ON v.Model_ID = m.Model_ID " +
                "WHERE v.Vehicle_ID = @Vehicle_ID";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Vehicle_ID", vehicleId);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                Console.WriteLine($"[VehicleRepo] getVehicleById={vehicleId} -> NOT FOUND");
                return null;
            }

            var vehicle = new Vehicle
            {
                Vehicle_ID = reader.GetInt32(reader.GetOrdinal("Vehicle_ID")),
                User_ID = reader.GetInt32(reader.GetOrdinal("User_ID")),
                Model_ID = reader.GetInt32(reader.GetOrdinal("Model_ID")),
                Vin = reader["Vin"] as string ?? string.Empty,
                License_Plate = reader["License_Plate"] as string ?? string.Empty,
                Model_Name = reader["Model_Name"] as string,
                Brand = reader["Brand"] as string,
                Battery_Type = reader["Battery_Type"] as string
            };

            Console.WriteLine($"[VehicleRepo] getVehicleById={vehicleId} -> model={vehicle.Model_Name}, plate={vehicle.License_Plate}");

            return vehicle;
        }
    }
}
