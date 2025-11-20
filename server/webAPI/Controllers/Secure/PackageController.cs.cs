using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webAPI.Services;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly PackageService _svc;
        private readonly DriverPackageService _driverPkgSvc;

        public PackageController(PackageService svc, DriverPackageService driverPkgSvc)
        {
            _svc = svc;
            _driverPkgSvc = driverPkgSvc;
        }

        /// <summary>
        /// GET /api/getpackages
        /// Trả về: { "status": "success", "data": [...] }
        /// </summary>
        [HttpGet("api/getpackages")]
        [AllowAnonymous] // giống servlet Java: endpoint public
        public async Task<IActionResult> Get()
        {
            try
            {
                var packages = await _svc.GetAllActivePackagesAsync();
                // Trả về rỗng vẫn coi là success để FE dễ xử lý
                return Ok(new { status = "success", data = packages });
            }
            catch (Exception e)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Server error: " + e.Message
                });
            }
        }

        /// <summary>
        /// GET /api/checkpackage?userId=123
        /// Returns { status: "success", data: true|false }
        /// </summary>
        [HttpGet("api/checkpackage")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckPackage([FromQuery] int userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { status = "error", message = "Missing or invalid userId" });

                var has = await _driverPkgSvc.ExistsAsync(userId);
                return Ok(new { status = "success", data = has });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = "Server error: " + e.Message });
            }
        }

        // (tuỳ chọn) OPTIONS — thường không cần nếu CORS đã cấu hình global trong Program.cs
        [HttpOptions("api/getpackages")]
        [AllowAnonymous]
        public IActionResult Options() => NoContent();
    }
}
