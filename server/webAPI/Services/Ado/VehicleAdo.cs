// Services/Ado/VehicleAdo.cs
using Dapper;
using Microsoft.Data.SqlClient;

namespace webAPI.Services.Ado
{
    public class VehicleAdo
    {
        public async Task<bool> UserOwnsVehicleAsync(
            SqlConnection con, SqlTransaction? tx, int userId, int vehicleId, CancellationToken ct)
        {
            const string sql = "SELECT 1 FROM Vehicle WHERE Vehicle_ID = @vid AND User_ID = @uid";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@vid", vehicleId);
            cmd.Parameters.AddWithValue("@uid", userId);
            var r = await cmd.ExecuteScalarAsync(ct);
            return r != null && r != DBNull.Value;
        }

        public async Task<string?> GetBatteryTypeByVehicleIdAsync(
            SqlConnection con, SqlTransaction? tx, int vehicleId, CancellationToken ct)
        {
            const string sql = @"
SELECT m.Battery_Type
FROM Vehicle v
JOIN Vehicle_Model m ON v.Model_ID = m.Model_ID
WHERE v.Vehicle_ID = @vid";

            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@vid", vehicleId);
            var r = await cmd.ExecuteScalarAsync(ct);
            return r == null || r == DBNull.Value ? null : Convert.ToString(r);
        }

        /// <summary>
        /// Trả về (ModelName, Plate) để dựng 'Model — Biển số'
        /// </summary>
        public async Task<(string? ModelName, string? Plate)?> GetVehicleByIdAsync(
            SqlConnection con, SqlTransaction? tx, int vehicleId, CancellationToken ct)
        {
            const string sql = @"
SELECT v.License_Plate, m.Model_Name
FROM Vehicle v
JOIN Vehicle_Model m ON v.Model_ID = m.Model_ID
WHERE v.Vehicle_ID = @vid";

            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@vid", vehicleId);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (!await rd.ReadAsync(ct)) return null;

            var model = rd["Model_Name"] as string;
            var plate = rd["License_Plate"] as string;
            return (model, plate);
        }

        /// <summary>
        /// Danh sách xe theo user (JOIN model) để trả về cho /api/secure/my-vehicles
        /// </summary>
        public async Task<List<webAPI.Models.VehicleListItem>> GetVehiclesByUserIdAsync(
            SqlConnection conn, SqlTransaction? tx, int userId, CancellationToken ct)
        {
            const string sql = @"
SELECT v.Vehicle_ID, v.User_ID, v.Model_ID, v.Vin, v.License_Plate,
       m.Model_Name, m.Brand, m.Battery_Type
FROM Vehicle v
JOIN Vehicle_Model m ON v.Model_ID = m.Model_ID
WHERE v.User_ID = @userId";

            var rows = await conn.QueryAsync<webAPI.Models.VehicleListItem>(
                new CommandDefinition(sql, new { userId }, tx, cancellationToken: ct));

            return rows.ToList();
        }
    }
}
