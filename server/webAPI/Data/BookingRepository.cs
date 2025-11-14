using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;   // ⬅️ dùng Microsoft.Data.SqlClient (không cần System.Data.SqlClient)
using webAPI.Models;
namespace webAPI.Data
{
    public class BookingRepository
    {
        public async Task<int> GetStationIdByNameAsync(SqlConnection con, string stationName)
        {
            const string sql = "SELECT Station_ID FROM dbo.Station WHERE Name = @Name";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Name", stationName);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value
                ? Convert.ToInt32(result)
                : -1;
        }

        public async Task<int> CountActiveBookingsAsync(SqlConnection con, int userId, DateTime now)
        {
            const string sql =
                "SELECT COUNT(1) " +
                "FROM dbo.Booking " +
                "WHERE User_ID = @UserId " +
                "  AND Status IN ('Reserved') " +
                "  AND Expired_Date > @Now";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Now", now);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value
                ? Convert.ToInt32(result)
                : 0;
        }

        public async Task<int> InsertBookingAsync(SqlConnection con, Booking b)
        {
            const string sql =
                "INSERT INTO dbo.Booking (" +
                "  User_ID, Vehicle_ID, Package_ID," +
                "  Station_ID, ChargingStation_ID, Slot_ID," +
                "  Battery_Request, Status, Booking_Time, Expired_Date, Qr_Code" +
                ") OUTPUT INSERTED.Booking_ID " +
                "VALUES (@User_ID, @Vehicle_ID, @Package_ID," +
                "        @Station_ID, @ChargingStation_ID, @Slot_ID," +
                "        @Battery_Request, @Status, @Booking_Time, @Expired_Date, @Qr_Code);";

            using var cmd = new SqlCommand(sql, con);
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

            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value
                ? Convert.ToInt32(result)
                : -1;
        }

        public async Task UpdateQRCodeAsync(SqlConnection con, int bookingId, string base64)
        {
            const string sql = "UPDATE dbo.Booking SET Qr_Code = @Qr_Code WHERE Booking_ID = @Booking_ID";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Qr_Code", base64);
            cmd.Parameters.AddWithValue("@Booking_ID", bookingId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
