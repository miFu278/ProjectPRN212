using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using webAPI.Models;
using webAPI.Services.Ado;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Produces("application/json")] // <-- thêm
    public class MyVehiclesController : ControllerBase
    {
        private readonly BatterySwapContext _db;
        private readonly VehicleAdo _vehicleAdo;

        public MyVehiclesController(BatterySwapContext db, VehicleAdo vehicleAdo)
        {
            _db = db;
            _vehicleAdo = vehicleAdo;
        }

        [HttpOptions("api/secure/my-vehicles")]
        public IActionResult Options() => NoContent();

        [Authorize]
        [HttpGet("api/secure/my-vehicles")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            // ❌ Đừng set Response.ContentType ở đây
            // Response.ContentType = "application/json; charset=utf-8";

            var sub = User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
            if (!int.TryParse(sub, out var userId))
                return Unauthorized(new { error = "Unauthorized" });

            await using var conn = (SqlConnection)_db.Database.GetDbConnection();
            await conn.OpenAsync(ct);

            try
            {
                var list = await _vehicleAdo.GetVehiclesByUserIdAsync(conn, null, userId, ct);
                return Ok(list ?? new List<VehicleListItem>()); // <-- đúng kiểu
            }
            catch (Exception ex)
            {
                // log ex nếu cần
                return StatusCode(500, new { error = "Failed to load vehicles" });
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
