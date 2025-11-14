// Controllers/Secure/PingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/secure/[controller]")]
    [Authorize]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(new
            {
                ok = true,
                name = User.Identity?.Name,           // map từ sub (email) nếu set NameClaimType
                role = User.FindFirst("role")?.Value, // “Driver”, “Staff”, …
                id = User.FindFirst("id")?.Value,
                claims
            });
        }
    }
}
