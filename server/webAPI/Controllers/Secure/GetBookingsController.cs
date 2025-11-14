using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace webAPI.Controllers
{
    [ApiController]
    [Route("api/secure/[controller]")] // => /api/secure/GetBookings
    [Authorize]
    public class GetBookingsController : ControllerBase
    {
        private readonly string _connectionString;

        public GetBookingsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BatterySwapDb")
                               ?? throw new InvalidOperationException("Missing connection string 'BatterySwapDb'");
        }

        /* ================== OPTIONS ================== */
        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            Response.Headers["Access-Control-Allow-Credentials"] = "true";

            // KHÔNG cần set ContentType ở đây cũng được
            return Ok(Array.Empty<object>());
        }

        /* ================== GET ================== */
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // KHÔNG set Response.ContentType ở đây nữa
            Response.Headers["Cache-Control"] = "no-store";

            try
            {
                // ===== Lấy userId & role từ JWT =====
                var userIdClaim =
                    User.FindFirst("id") ??
                    User.FindFirst("jwt_id") ??
                    User.FindFirst(ClaimTypes.NameIdentifier);

                var roleClaim =
                    User.FindFirst(ClaimTypes.Role) ??
                    User.FindFirst("role") ??
                    User.FindFirst("jwt_role");

                if (userIdClaim == null || roleClaim == null)
                {
                    return Unauthorized(new { error = "Unauthorized: missing JWT context" });
                }

                if (!int.TryParse(userIdClaim.Value, out var jwtUserId))
                {
                    return Unauthorized(new { error = "Unauthorized: invalid user id" });
                }

                var jwtRole = roleClaim.Value ?? string.Empty;
                var isPrivileged = jwtRole.Equals("staff", StringComparison.OrdinalIgnoreCase)
                                   || jwtRole.Equals("admin", StringComparison.OrdinalIgnoreCase);

                const string baseSelect =
                    @"SELECT b.Booking_ID, b.User_ID, b.Vehicle_ID, b.Package_ID,
                             b.Station_ID, b.ChargingStation_ID, b.Slot_ID,
                             b.Battery_Request, b.Status, b.Booking_Time, b.Expired_Date, b.Qr_Code,
                             u.FullName AS User_Name,
                             v.License_Plate AS Vehicle_License,
                             vm.Model_Name AS Vehicle_ModelName,
                             p.[Name] AS Package_Name,
                             s.Name AS Station_Name,
                             cs.Name AS ChargingStation_Name,
                             bs.Slot_Code
                      FROM dbo.Booking b
                      JOIN dbo.Users u ON u.ID = b.User_ID
                      JOIN dbo.Vehicle v ON v.Vehicle_ID = b.Vehicle_ID
                      JOIN dbo.Vehicle_Model vm ON vm.Model_ID = v.Model_ID
                      JOIN dbo.Package p ON p.Package_ID = b.Package_ID
                      LEFT JOIN dbo.Charging_Station cs ON cs.ChargingStation_ID = b.ChargingStation_ID
                      LEFT JOIN dbo.Station s ON s.Station_ID = cs.Station_ID
                      LEFT JOIN dbo.BatterySlot bs ON bs.Slot_ID = b.Slot_ID ";

                const string orderBy = " ORDER BY b.Booking_Time DESC";

                string sql = isPrivileged
                    ? baseSelect + orderBy
                    : baseSelect + "WHERE b.User_ID = @User_ID " + orderBy;

                var result = new List<BookingDto>();

                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    if (!isPrivileged)
                    {
                        cmd.Parameters.Add("@User_ID", SqlDbType.Int).Value = jwtUserId;
                    }

                    await con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var dto = new BookingDto
                            {
                                BookingId = reader.GetInt32(reader.GetOrdinal("Booking_ID")),
                                UserId = reader.GetInt32(reader.GetOrdinal("User_ID")),
                                VehicleId = reader.GetInt32(reader.GetOrdinal("Vehicle_ID")),
                                PackageId = reader.GetInt32(reader.GetOrdinal("Package_ID")),
                                StationId = NullSafeInt(reader, "Station_ID"),
                                ChargingStationId = NullSafeInt(reader, "ChargingStation_ID"),
                                SlotId = NullSafeInt(reader, "Slot_ID"),

                                UserName = NullSafeStr(reader, "User_Name"),
                                VehicleLicense = NullSafeStr(reader, "Vehicle_License"),
                                VehicleModel = NullSafeStr(reader, "Vehicle_ModelName"),
                                PackageName = NullSafeStr(reader, "Package_Name"),
                                StationName = NullSafeStr(reader, "Station_Name"),
                                ChargingStationName = NullSafeStr(reader, "ChargingStation_Name"),
                                SlotCode = NullSafeStr(reader, "Slot_Code"),

                                BatteryModelRequested = NullSafeStr(reader, "Battery_Request"),
                                Status = NullSafeStr(reader, "Status"),
                                BookingTime = NullSafeTs(reader, "Booking_Time"),
                                ExpiredDate = NullSafeTs(reader, "Expired_Date"),
                                QrCode = NullSafeStr(reader, "Qr_Code")
                            };

                            result.Add(dto);
                        }
                    }
                }

                // ASP.NET Core tự chọn JSON formatter & Content-Type
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        /* ================== Helpers ================== */

        private static string NullSafeStr(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal)) return string.Empty;
            return reader.GetString(ordinal);
        }

        private static string? NullSafeTs(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal)) return string.Empty;

            var dt = reader.GetDateTime(ordinal);
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static int? NullSafeInt(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal)) return null;
            return reader.GetInt32(ordinal);
        }
    }

    public class BookingDto
    {
        // ids
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int VehicleId { get; set; }
        public int PackageId { get; set; }
        public int? StationId { get; set; }
        public int? ChargingStationId { get; set; }
        public int? SlotId { get; set; }

        // display
        public string UserName { get; set; } = "";
        public string VehicleLicense { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public string PackageName { get; set; } = "";
        public string StationName { get; set; } = "";
        public string ChargingStationName { get; set; } = "";
        public string SlotCode { get; set; } = "";

        // booking content
        public string BatteryModelRequested { get; set; } = "";
        public string Status { get; set; } = "";
        public string? BookingTime { get; set; }
        public string? ExpiredDate { get; set; }
        public string QrCode { get; set; } = "";
    }
}
