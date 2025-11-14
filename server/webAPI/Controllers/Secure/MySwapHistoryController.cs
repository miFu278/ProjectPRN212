using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/secure/my-swaps")]
    [Authorize]
    public class MySwapHistoryController : ControllerBase
    {
        private readonly string _connectionString;

        public MySwapHistoryController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BatterySwapDb")
                               ?? throw new InvalidOperationException("Missing DB connection string 'BatterySwapDb'");
        }

        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            Response.Headers["Access-Control-Allow-Credentials"] = "true";
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? from = null, [FromQuery] string? to = null)
        {
            try
            {
                // ==== LẤY CLAIM CHUẨN TỪ JWT ====
                var roleClaim =
                    User.FindFirst(ClaimTypes.Role) ??
                    User.FindFirst("role") ??
                    User.FindFirst("jwt_role");

                var idClaim =
                    User.FindFirst("id") ??                // ✅ claim thật trong token
                    User.FindFirst("jwt_id") ??
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (idClaim == null || roleClaim == null)
                    return Unauthorized(new { error = "Missing JWT claims" });

                var role = roleClaim.Value ?? "";
                if (!role.Equals("Driver", StringComparison.OrdinalIgnoreCase))
                    return Forbid("Driver only");

                if (!int.TryParse(idClaim.Value, out var userId))
                    return Unauthorized(new { error = "Invalid user id in token" });

                // ==== SQL ĐÚNG THEO SCHEMA SwapTransaction ====
                string sql = @"
                    SELECT st.ID,
                           st.Driver_ID,
                           st.Station_ID,
                           st.Old_Battery,
                           st.New_Battery,
                           st.SoH_Old,
                           st.SoH_New,
                           st.Fee,
                           st.Payment_ID,
                           st.Status,
                           st.Swap_Time,
                           st.Booking_ID,
                           st.ChargingStation_ID,
                           s.Name AS Station_Name
                    FROM dbo.SwapTransaction st
                    JOIN dbo.Station s ON s.Station_ID = st.Station_ID
                    WHERE st.Driver_ID = @User_ID";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@User_ID", SqlDbType.Int) { Value = userId }
                };

                if (!string.IsNullOrWhiteSpace(from))
                {
                    sql += " AND st.Swap_Time >= @From";
                    parameters.Add(new SqlParameter("@From", SqlDbType.DateTime)
                    {
                        Value = DateTime.Parse(from)
                    });
                }
                if (!string.IsNullOrWhiteSpace(to))
                {
                    sql += " AND st.Swap_Time <= @To";
                    parameters.Add(new SqlParameter("@To", SqlDbType.DateTime)
                    {
                        Value = DateTime.Parse(to)
                    });
                }

                sql += " ORDER BY st.Swap_Time DESC";

                var result = new List<SwapHistoryDto>();
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await conn.OpenAsync();
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    result.Add(new SwapHistoryDto
                    {
                        Id = rd.GetInt32(rd.GetOrdinal("ID")),
                        DriverId = rd.GetInt32(rd.GetOrdinal("Driver_ID")),
                        StationId = rd.GetInt32(rd.GetOrdinal("Station_ID")),
                        StationName = rd["Station_Name"]?.ToString() ?? "",
                        OldBattery = NullSafeInt(rd, "Old_Battery"),
                        NewBattery = NullSafeInt(rd, "New_Battery"),
                        SoH_Old = NullSafeDouble(rd, "SoH_Old"),
                        SoH_New = NullSafeDouble(rd, "SoH_New"),
                        Fee = NullSafeDecimal(rd, "Fee"),
                        PaymentId = NullSafeInt(rd, "Payment_ID"),
                        BookingId = NullSafeInt(rd, "Booking_ID"),
                        ChargingStationId = NullSafeInt(rd, "ChargingStation_ID"),
                        Status = rd["Status"]?.ToString() ?? "",
                        SwapTime = rd["Swap_Time"] is DateTime dt
                                            ? dt.ToString("yyyy-MM-dd HH:mm:ss")
                                            : ""
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        /* ===== Helpers đọc nullable ===== */

        private static int? NullSafeInt(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return null;
            return rd.GetInt32(ord);
        }

        private static double? NullSafeDouble(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return null;
            return rd.GetDouble(ord);
        }

        private static decimal? NullSafeDecimal(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return null;
            return rd.GetDecimal(ord);
        }

        /* ===== DTO khớp với SwapTransaction + thêm StationName & SwapTime string ===== */
        public class SwapHistoryDto
        {
            public int Id { get; set; }
            public int DriverId { get; set; }
            public int StationId { get; set; }
            public int? OldBattery { get; set; }
            public int? NewBattery { get; set; }
            public double? SoH_Old { get; set; }
            public double? SoH_New { get; set; }
            public decimal? Fee { get; set; }
            public int? PaymentId { get; set; }
            public string Status { get; set; } = "";
            public string SwapTime { get; set; } = "";   // format yyyy-MM-dd HH:mm:ss
            public int? BookingId { get; set; }
            public int? ChargingStationId { get; set; }
            public string StationName { get; set; } = "";
        }
    }
}
