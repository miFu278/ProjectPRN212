using Microsoft.AspNetCore.Mvc;
using webAPI.Services;
using webAPI.Models;

namespace webAPI.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UsersService _users;
        private readonly JwtTokenService _jwt;

        public AuthController(UsersService users, JwtTokenService jwt)
        {
            _users = users;
            _jwt = jwt;
        }

        [HttpPost("api/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.password))
                return BadRequest(new { status = "fail", code = "AUTH_MISSING_FIELD", message = "Thiếu email hoặc mật khẩu" });

            var user = await _users.CheckLoginAsync(req.email.Trim(), req.password);
            if (user == null)
                return Unauthorized(new { status = "fail", code = "AUTH_INVALID", message = "Email hoặc mật khẩu không hợp lệ" });

            if (string.Equals(user.Status, "Blocked", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { status = "fail", code = "AUTH_BLOCKED", message = "Tài khoản bị khóa" });

            var token = _jwt.GenerateToken(user.Email ?? "", user.Role ?? "", user.ID);

            return Ok(new
            {
                status = "success",
                token,
                user = new
                {
                    id = user.ID,
                    fullName = user.FullName,
                    phone = user.Phone,
                    email = user.Email,
                    role = user.Role,
                    status = user.Status,
                    stationId = user.Station_ID,
                    avatar_URL = user.Avatar_URL
                }
            });
        }

        public class LoginRequest
        {
            public string email { get; set; } = "";
            public string password { get; set; } = "";
        }
    }
}
