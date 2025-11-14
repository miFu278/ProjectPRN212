using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/secure/my-packages")]
    [Authorize]
    public class MyPackageHistoryController : ControllerBase
    {
        private readonly string _connectionString;

        public MyPackageHistoryController(IConfiguration configuration)
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
                // ===== Lấy claim CHUẨN =====
                var roleClaim =
                    User.FindFirst(ClaimTypes.Role) ??
                    User.FindFirst("role") ??
                    User.FindFirst("jwt_role");

                var idClaim =
                    User.FindFirst("id") ??          // ✅ claim thật trong token
                    User.FindFirst("jwt_id") ??
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (idClaim == null || roleClaim == null)
                    return Unauthorized(new { error = "Missing JWT claims" });

                if (!int.TryParse(idClaim.Value, out var userId))
                    return Unauthorized(new { error = "Invalid user id in token" });

                var role = roleClaim.Value ?? "";
                if (!role.Equals("Driver", StringComparison.OrdinalIgnoreCase))
                    return Forbid("Driver only");

                // Parse from/to giống Java
                DateTime? fromDt = ParseFlexibleStart(from);
                DateTime? toDt = ParseFlexibleEndInclusive(to);

                string sql = @"
                    SELECT pt.ID,
                           pt.User_ID,
                           pt.Station_ID,
                           pt.Package_ID,
                           pt.Amount,
                           pt.Payment_Method,
                           pt.Description,
                           pt.Transaction_Time,
                           p.Name AS Package_Name
                    FROM dbo.PaymentTransaction pt
                    LEFT JOIN dbo.Package p ON p.Package_ID = pt.Package_ID
                    WHERE pt.User_ID = @User_ID
                      AND pt.Package_ID IS NOT NULL";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@User_ID", SqlDbType.Int) { Value = userId }
                };

                if (fromDt.HasValue)
                {
                    sql += " AND pt.Transaction_Time >= @From";
                    parameters.Add(new SqlParameter("@From", SqlDbType.DateTime2)
                    {
                        Value = fromDt.Value
                    });
                }
                if (toDt.HasValue)
                {
                    sql += " AND pt.Transaction_Time <= @To";
                    parameters.Add(new SqlParameter("@To", SqlDbType.DateTime2)
                    {
                        Value = toDt.Value
                    });
                }

                sql += " ORDER BY pt.Transaction_Time DESC";

                var result = new List<PackageHistoryDto>();

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await conn.OpenAsync();
                using var rd = await cmd.ExecuteReaderAsync();

                while (await rd.ReadAsync())
                {
                    var dto = new PackageHistoryDto
                    {
                        Id = rd.GetInt32(rd.GetOrdinal("ID")),
                        UserId = rd.GetInt32(rd.GetOrdinal("User_ID")),
                        StationId = NullSafeInt(rd, "Station_ID"),
                        PackageId = NullSafeInt(rd, "Package_ID"),
                        PackageName = NullSafeStr(rd, "Package_Name"),
                        Amount = NullSafeDecimal(rd, "Amount"),
                        PaymentMethod = NullSafeStr(rd, "Payment_Method"),
                        Description = NullSafeStr(rd, "Description"),
                        TransactionTime = NullSafeDateTimeStr(rd, "Transaction_Time")
                    };

                    result.Add(dto);
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    error = "Invalid 'from'/'to' format",
                    hint = "Supported: yyyy-MM-dd | yyyy-MM-ddTHH:mm:ss | yyyy-MM-dd HH:mm:ss",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        /* ===== Helpers ===== */

        private static int? NullSafeInt(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return null;
            return rd.GetInt32(ord);
        }

        private static string NullSafeStr(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return string.Empty;
            return rd.GetString(ord);
        }

        private static decimal NullSafeDecimal(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return 0m;
            return rd.GetDecimal(ord);
        }

        private static string NullSafeDateTimeStr(SqlDataReader rd, string column)
        {
            var ord = rd.GetOrdinal(column);
            if (rd.IsDBNull(ord)) return string.Empty;
            var dt = rd.GetDateTime(ord);
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static DateTime? ParseFlexibleStart(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (DateTime.TryParseExact(input, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                return d1.Date;

            if (DateTime.TryParseExact(input, "yyyy-MM-dd'T'HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                return d2;

            if (DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d3))
                return d3;

            throw new ArgumentException("Unsupported datetime format: " + input);
        }

        private static DateTime? ParseFlexibleEndInclusive(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (DateTime.TryParseExact(input, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                return d1.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            if (DateTime.TryParseExact(input, "yyyy-MM-dd'T'HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                return d2;

            if (DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d3))
                return d3;

            throw new ArgumentException("Unsupported datetime format: " + input);
        }

        public class PackageHistoryDto
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int? StationId { get; set; }
            public int? PackageId { get; set; }
            public string PackageName { get; set; } = "";
            public decimal Amount { get; set; }
            public string PaymentMethod { get; set; } = "";
            public string Description { get; set; } = "";
            public string TransactionTime { get; set; } = "";
        }
    }
}
