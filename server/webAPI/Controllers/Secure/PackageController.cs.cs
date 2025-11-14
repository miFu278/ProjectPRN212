using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webAPI.Services;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly PackageService _svc;

        public PackageController(PackageService svc)
        {
            _svc = svc;
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

        // (tuỳ chọn) OPTIONS — thường không cần nếu CORS đã cấu hình global trong Program.cs
        [HttpOptions("api/getpackages")]
        [AllowAnonymous]
        public IActionResult Options() => NoContent();
    }
}
