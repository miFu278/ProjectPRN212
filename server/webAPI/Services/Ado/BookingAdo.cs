// Services/Ado/BookingAdo.cs
using Microsoft.Data.SqlClient;
using System.Data;
using webAPI.Models;

namespace webAPI.Services.Ado
{
    public class BookingAdo
    {
        public async Task<int> GetStationIdByNameAsync(SqlConnection con, SqlTransaction tx, string stationName, CancellationToken ct)
        {
            const string sql = "SELECT Station_ID FROM dbo.Station WHERE Name = @name";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 255) { Value = stationName });
            var r = await cmd.ExecuteScalarAsync(ct);
            return (r == null || r is DBNull) ? -1 : Convert.ToInt32(r);
        }

        public async Task<int> CountActiveBookingsAsync(SqlConnection con, SqlTransaction tx, int userId, DateTime now, CancellationToken ct)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Booking
WHERE User_ID = @uid
  AND Status IN (N'Reserved')
  AND Expired_Date > @now";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.Add(new SqlParameter("@uid", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@now", SqlDbType.DateTime2) { Value = now });
            var r = await cmd.ExecuteScalarAsync(ct);
            return (r == null || r is DBNull) ? 0 : Convert.ToInt32(r);
        }

        public async Task<int> InsertBookingAsync(SqlConnection con, SqlTransaction tx, Models.Booking b, CancellationToken ct)
        {
            const string sql = @"
INSERT INTO dbo.Booking (
  User_ID, Vehicle_ID, Package_ID,
  Station_ID, ChargingStation_ID, Slot_ID,
  Battery_Request, Status, Booking_Time, Expired_Date, Qr_Code
) OUTPUT INSERTED.Booking_ID
VALUES (@User_ID, @Vehicle_ID, @Package_ID,
        @Station_ID, @ChargingStation_ID, @Slot_ID,
        @Battery_Request, @Status, @Booking_Time, @Expired_Date, @Qr_Code);";

            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@User_ID", b.User_ID);
            cmd.Parameters.AddWithValue("@Vehicle_ID", b.Vehicle_ID);
            cmd.Parameters.AddWithValue("@Package_ID", b.Package_ID);
            cmd.Parameters.AddWithValue("@Station_ID", b.Station_ID);
            cmd.Parameters.AddWithValue("@ChargingStation_ID", b.ChargingStation_ID);
            cmd.Parameters.AddWithValue("@Slot_ID", b.Slot_ID);
            cmd.Parameters.AddWithValue("@Battery_Request", (object?)b.Battery_Request ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", b.Status);
            cmd.Parameters.AddWithValue("@Booking_Time", b.Booking_Time);
            cmd.Parameters.AddWithValue("@Expired_Date", b.Expired_Date);
            cmd.Parameters.AddWithValue("@Qr_Code", (object?)b.Qr_Code ?? DBNull.Value);

            var r = await cmd.ExecuteScalarAsync(ct);
            return (r == null || r is DBNull) ? -1 : Convert.ToInt32(r);
        }

        public async Task UpdateQRCodeAsync(SqlConnection con, SqlTransaction tx, int bookingId, string base64, CancellationToken ct)
        {
            const string sql = "UPDATE dbo.Booking SET Qr_Code=@qr WHERE Booking_ID=@id";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@qr", base64);
            cmd.Parameters.AddWithValue("@id", bookingId);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<(string? ModelName, string? Plate)> GetVehicleLabelAsync(SqlConnection con, SqlTransaction tx, int vehicleId, CancellationToken ct)
        {
            const string sql = @"
SELECT v.License_Plate, m.Model_Name
FROM Vehicle v
JOIN Vehicle_Model m ON v.Model_ID = m.Model_ID
WHERE v.Vehicle_ID = @vid";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@vid", vehicleId);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (!await rd.ReadAsync(ct)) return (null, null);
            var plate = rd["License_Plate"] as string;
            var model = rd["Model_Name"] as string;
            return (model, plate);
        }
    }
}
