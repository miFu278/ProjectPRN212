using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using System.Linq;
using webAPI.Models;
using webAPI.Services;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/secure/[controller]")]
    [Authorize] // yêu cầu JWT
    public class VehicleConfirmController : ControllerBase
    {
        private static readonly string[] VALID_MODELS = new[]
        {
            "Gogoro SuperSport",
            "Gogoro 2 Delight",
            "Gogoro Viva Mix",
            "Gogoro CrossOver S",
            "Gogoro S2 ABS"
        };

        private readonly VehicleService _vehicleSvc;

        public VehicleConfirmController(VehicleService vehicleSvc)
        {
            _vehicleSvc = vehicleSvc;
        }

        public class ConfirmRequest
        {
            public string? vin { get; set; }
            public string? licensePlate { get; set; }
            public string? owner { get; set; }         // có dấu (nếu có) – hiện chưa dùng
            public string? ownerNoMark { get; set; }   // không dấu (tuỳ FE) – hiện chưa dùng
            public string? model { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConfirmRequest body)
        {
            // ===== 1. Lấy claim từ JWT =====
            var role = User.FindFirst("role")?.Value;
            var idStr = User.FindFirst("id")?.Value;

            if (role == null || idStr == null)
                return Unauthorized(new { status = "error", message = "Unauthorized" });

            if (!string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (!int.TryParse(idStr, out var userId))
                return Unauthorized(new { status = "error", message = "Invalid token" });

            // ===== 2. Normalize input =====
            var vin = body.vin?.Trim().ToUpperInvariant();
            var licensePlate = NormalizePlate(body.licensePlate);
            var model = body.model?.Trim();

            // Validate VIN
            if (!IsValidVIN(vin))
                return BadRequest(new { status = "error", message = "VIN không hợp lệ (phải 17 ký tự chuẩn)" });

            // Validate biển số
            if (!IsValidPlate(licensePlate))
                return BadRequest(new { status = "error", message = "Biển số không hợp lệ" });

            // Validate model
            if (string.IsNullOrWhiteSpace(model))
                return BadRequest(new { status = "error", message = "Model không được để trống" });

            var supported = VALID_MODELS.Any(m => m.Equals(model, StringComparison.OrdinalIgnoreCase));
            if (!supported)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Model không được hỗ trợ",
                    acceptedModels = VALID_MODELS
                });
            }

            try
            {
                // ===== 3. Check VIN trùng =====
                var vinExists = await _vehicleSvc.IsVinExistsAsync(vin!);
                if (vinExists)
                {
                    return BadRequest(new
                    {
                        status = "error",
                        message = "VIN này đã tồn tại trong hệ thống"
                    });
                }

                // ===== 4. Map Model_Name -> Model_ID =====
                var modelId = await _vehicleSvc.GetModelIdByNameAsync(model);
                if (modelId is null)
                    return BadRequest(new { status = "error", message = "Không tìm thấy model trong DB" });

                // ===== 5. Tạo entity Vehicle (DTO mới) =====
                var v = new Vehicle
                {
                    User_ID = userId,
                    Model_ID = modelId.Value,  // ⬅️ int? -> int
                    Vin = vin!,                // vin đã được validate not null ở trên
                    License_Plate = licensePlate ?? string.Empty
                };

                // ===== 6. Insert DB =====
                var ok = await _vehicleSvc.InsertVehicleAsync(v);
                if (!ok)
                    return StatusCode(500, new { status = "error", message = "Không thể lưu xe vào DB" });

                // Có 2 option:
                //  a) trả về 1 xe vừa insert
                //  b) trả về danh sách xe của user
                // Mình chọn (b) cho FE dễ sync list
                var vehicles = await _vehicleSvc.GetVehiclesByUserIdAsync(userId);

                return Ok(new
                {
                    status = "success",
                    message = "Liên kết xe thành công",
                    data = vehicles
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi hệ thống: " + e.Message });
            }
        }

        // ==== Helpers (port từ Java) ====

        private static string? ExtractModel(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // tìm theo nhãn "Model / Số loại / Model code: ..."
            var modelPattern = new Regex("(?i)(Model|S[ốo]\\s*loai|Model\\s*code)[:\\s]+([^\\n\\r]{3,40})");
            var m = modelPattern.Match(text);
            if (m.Success)
            {
                var detected = m.Groups[2].Value.Trim();
                foreach (var model in VALID_MODELS)
                {
                    if (detected.Contains(model, StringComparison.OrdinalIgnoreCase))
                        return model;
                }
            }

            // fallback: tìm substring chứa tên các model đã biết
            var normalized = text.ToLowerInvariant();
            foreach (var model in VALID_MODELS)
            {
                if (normalized.Contains(model.ToLowerInvariant()))
                    return model;
            }
            return null;
        }

        private static bool IsValidVIN(string? vin)
        {
            return vin != null && Regex.IsMatch(vin.Trim().ToUpperInvariant(), "^[A-HJ-NPR-Z0-9]{17}$");
        }

        private static bool IsValidPlate(string? plate)
        {
            if (plate == null) return false;
            return Regex.IsMatch(plate, "^[0-9]{2}[A-Z]{1,2}[0-9]{1}-[0-9]{4,6}$");
        }

        private static string? NormalizePlate(string? raw)
        {
            if (raw == null) return null;
            var p = raw.ToUpperInvariant().Replace(" ", "").Replace(".", "");
            var m = Regex.Match(p, "^([0-9]{2}[A-Z]{1,2}[0-9]{1})([0-9]{4,6})$");
            if (m.Success) p = m.Groups[1].Value + "-" + m.Groups[2].Value;
            return p;
        }

        // (nếu cần) bỏ dấu & khoảng trắng
        private static string ToNoMarkNoSpace(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var noMark = sb.ToString().Normalize(NormalizationForm.FormC);
            return Regex.Replace(noMark, "[^A-Za-z0-9]", "").ToLowerInvariant();
        }
    }
}
