using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/secure/[controller]")]
    [Authorize] // yêu cầu JWT
    public class VehicleOcrController : ControllerBase
    {
        private const string API_KEY = "K87030538488957"; // thay nếu cần
        private const string OCR_URL = "https://api.ocr.space/parse/image";

        private static readonly string[] VALID_MODELS = new[]
        {
            "Gogoro SuperSport",
            "Gogoro 2 Delight",
            "Gogoro Viva Mix",
            "Gogoro CrossOver S",
            "Gogoro S2 ABS"
        };

        // ===== Helpers: lấy claim an toàn khi mapping khác nhau =====
        private static string? GetRoleClaim(ClaimsPrincipal user)
        {
            // 1) tên thuần "role" (khi đã MapInboundClaims=false)
            var v = user.FindFirst("role")?.Value;
            if (!string.IsNullOrEmpty(v)) return v;

            // 2) chuẩn .NET
            v = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(v)) return v;

            // 3) fallback type URI phổ biến
            v = user.Claims.FirstOrDefault(c => c.Type.EndsWith("/claims/role", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrEmpty(v)) return v;

            return null;
        }

        private static string? GetUserIdClaim(ClaimsPrincipal user)
        {
            // custom "id" của bạn
            var v = user.FindFirst("id")?.Value;
            if (!string.IsNullOrEmpty(v)) return v;

            // fallback (tuỳ đội khác gắn)
            v = user.FindFirst("userid")?.Value;
            if (!string.IsNullOrEmpty(v)) return v;

            return null;
        }

        // Form-model để Swagger hiểu upload file
        public class OcrUploadForm
        {
            // tên field phải khớp FE: carDoc
            public IFormFile? CarDoc { get; set; }
        }

        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [Consumes("multipart/form-data")] // ⭐ quan trọng cho Swagger
        public async Task<IActionResult> Post([FromForm] OcrUploadForm form)
        {
            // ===== Auth (đọc claim robust) =====
            var role = GetRoleClaim(User);
            var idStr = GetUserIdClaim(User);

            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(idStr))
            {
                // Trả debug khi dev để bạn thấy server đang nhận claim gì
                return Unauthorized(new
                {
                    status = "error",
                    message = "Unauthorized",
                    debug = new
                    {
                        isAuth = User.Identity?.IsAuthenticated,
                        claims = User.Claims.Select(c => new { c.Type, c.Value })
                    }
                });
            }

            if (!string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase))
                return Forbid(); // chỉ tài xế dùng chức năng này

            // ===== File =====
            var carDoc = form.CarDoc;
            if (carDoc == null || carDoc.Length == 0)
                return BadRequest(new { status = "error", message = "Vui lòng tải lên ảnh cà vẹt xe (carDoc)" });

            // ===== OCR =====
            string? parsedText = await CallOcrApiAsync(carDoc);
            if (string.IsNullOrWhiteSpace(parsedText))
                return BadRequest(new { status = "error", message = "OCR thất bại hoặc không đọc được thông tin" });

            // ===== Parse =====
            string raw = parsedText.Normalize(NormalizationForm.FormC);
            string foldedForPlate = FoldForPlate(raw);

            string? vin = ExtractVinRobust(raw);
            string? licensePlate = NormalizePlate(ExtractLicensePlate(foldedForPlate, raw));
            var ownerPair = ExtractOwnerVN(raw);
            string? detectedModel = ExtractModel(raw);

            var suggests = new Dictionary<string, object?>
            {
                ["owner"] = ownerPair.ownerNoMarks,
                ["ownerWithMarks"] = ownerPair.ownerWithMarks,
                ["vin"] = vin,
                ["licensePlate"] = licensePlate,
                ["model"] = detectedModel,
                ["modelSupported"] = detectedModel == null ? null :
                    VALID_MODELS.Any(m => m.Equals(detectedModel, StringComparison.OrdinalIgnoreCase))
            };

            var validity = new Dictionary<string, object?>
            {
                ["vin"] = IsValidVIN(vin),
                ["licensePlate"] = IsValidPlate(licensePlate)
            };

            return Ok(new
            {
                status = "ok",
                data = new
                {
                    rawText = raw,
                    suggests,
                    validity,
                    hints = new { acceptedModels = VALID_MODELS }
                }
            });
        }

        // ==== Call OCR.space ====
        private static async Task<string?> CallOcrApiAsync(IFormFile file)
        {
            using var http = new HttpClient();
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent("auto"), "language");
            content.Add(new StringContent("2"), "OCREngine");
            content.Add(new StringContent("true"), "scale");
            content.Add(new StringContent("false"), "isOverlayRequired");
            content.Add(new StringContent("true"), "detectOrientation");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            content.Add(new ByteArrayContent(ms.ToArray()), "file", file.FileName);

            http.DefaultRequestHeaders.Add("apikey", API_KEY);
            var resp = await http.PostAsync(OCR_URL, content);
            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("OCRExitCode", out var exit) && exit.GetInt32() == 1)
            {
                var parsed = root.GetProperty("ParsedResults")[0].GetProperty("ParsedText").GetString();
                return parsed?.Normalize(NormalizationForm.FormC);
            }
            return null;
        }

        // ===== Helpers / Extractors =====
        private static (string? ownerWithMarks, string? ownerNoMarks) ExtractOwnerVN(string rawNfc)
        {
            if (rawNfc == null) return (null, null);
            var nfc = rawNfc.Replace("\r\n", "\n");
            var nfd = nfc.Normalize(NormalizationForm.FormD);
            var noMarks = RemoveNonSpacingMarks(nfd);

            var label = new Regex("(?im)^(ten\\s*chu\\s*xe|chu\\s*xe|owner'?s?\\s*full\\s*name)\\s*[:：]?(.*)$");
            var linesNo = noMarks.Split('\n');
            var linesYes = nfc.Split('\n');

            for (int i = 0; i < linesNo.Length; i++)
            {
                if (!label.IsMatch(linesNo[i].Trim())) continue;

                for (int step = 0; step <= 2 && i + step < linesYes.Length; step++)
                {
                    var cand = step == 0 ? AfterLabelTakeName(linesYes[i]) : SanitizeNameLine(linesYes[i + step]);
                    if (string.IsNullOrWhiteSpace(cand)) continue;

                    var candNo = ToNoMark(cand).ToLowerInvariant();
                    if (candNo.Contains("so may") || candNo.Contains("engine")) continue;
                    if (Regex.Replace(cand, "[^0-9]", "").Length >= 3) continue;

                    var withMarks = SqueezeSpaces(cand).ToUpperInvariant();
                    var nomark = ToNoMark(withMarks);
                    return (withMarks, nomark);
                }
            }
            return (null, null);
        }

        private static string AfterLabelTakeName(string line)
        {
            var parts = new Regex("[:：]").Split(line ?? string.Empty, 2);
            var tail = parts.Length == 2 ? parts[1] : string.Empty;
            return SanitizeNameLine(tail);
        }

        private static string SanitizeNameLine(string s)
        {
            if (s == null) return "";
            s = Regex.Replace(s, "(?i)địa\\s*chỉ.*$", "");
            s = Regex.Replace(s, "(?i)dia\\s*chi.*$", "");
            s = Regex.Replace(s, "(?i)address.*$", "");
            s = Regex.Replace(s, "[|•·►]+", " ");
            s = Regex.Replace(s, "[^\\p{L}\\p{M}\\s'.-]", " ");
            s = SqueezeSpaces(s).Trim();
            return s;
        }

        private static string SqueezeSpaces(string s) => Regex.Replace(s, "\\s{2,}", " ");

        private static string ToNoMark(string s)
        {
            var nfd = s.Normalize(NormalizationForm.FormD);
            return RemoveNonSpacingMarks(nfd);
        }

        private static string RemoveNonSpacingMarks(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string FoldForPlate(string s)
        {
            if (s == null) return "";
            var nfd = s.Normalize(NormalizationForm.FormD);
            var noMarks = RemoveNonSpacingMarks(nfd);
            var x = noMarks;
            x = Regex.Replace(x, "(?i)blen\\s*so", "bien so");
            x = Regex.Replace(x, "(?i)bien\\s*s[o0dđ]", "bien so");
            x = Regex.Replace(x, "(?i)bien\\s*so", "bien so");
            x = x.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' ');
            x = Regex.Replace(x, "[\\x0B\\f]", " ");
            x = Regex.Replace(x, " {2,}", " ");
            x = Regex.Replace(x, "\\n{2,}", "\n");
            return x;
        }

        private static string? ExtractVinRobust(string raw)
        {
            if (raw == null) return null;
            var nfc = raw.Replace("\r\n", "\n");
            var nfd = nfc.Normalize(NormalizationForm.FormD);
            var noMarks = RemoveNonSpacingMarks(nfd);

            var linesYes = nfc.Split('\n');
            var linesNo = noMarks.Split('\n');

            var label = new Regex("(?i)\\b(so\\s*khung|chassis)\\b");
            for (int i = 0; i < linesNo.Length; i++)
            {
                if (!label.IsMatch(linesNo[i])) continue;

                string cand = AfterColonOrEmpty(linesYes[i]);
                if (string.IsNullOrWhiteSpace(cand) && i + 1 < linesYes.Length) cand = linesYes[i + 1];
                else if (!string.IsNullOrWhiteSpace(cand) && i + 1 < linesYes.Length) cand = cand + " " + linesYes[i + 1];

                var vin = TryPickVin(cand);
                if (vin != null) return vin;
            }

            var fallback = TryPickVin(nfc);
            if (fallback != null) return fallback;

            var m17 = Regex.Match(nfc.ToUpperInvariant(), "\\b([A-HJ-NPR-Z0-9]{17})\\b");
            return m17.Success ? m17.Groups[1].Value : null;
        }

        private static string AfterColonOrEmpty(string line)
        {
            var parts = new Regex("[:：]").Split(line ?? string.Empty, 2);
            return parts.Length == 2 ? parts[1] : string.Empty;
        }

        private static string? TryPickVin(string text)
        {
            if (text == null) return null;
            var up = Regex.Replace(text.ToUpperInvariant(), "[^A-Z0-9]", "");
            up = up.Replace('I', '1').Replace('L', '1').Replace('O', '0').Replace('Q', '0');

            for (int i = 0; i + 17 <= up.Length; i++)
            {
                var sub = up.Substring(i, 17);
                if (Regex.IsMatch(sub, "^[A-HJ-NPR-Z0-9]{17}$"))
                    return sub;
            }
            return null;
        }

        private static string? ExtractLicensePlate(string main, string raw)
        {
            var all = ((main ?? "") + " " + (raw ?? ""))
                .ToUpperInvariant()
                .Replace('\r', ' ')
                .Replace('\n', ' ');
            all = Regex.Replace(all, "\\s{2,}", " ");

            var motor = new Regex("\\b([0-9]{2})([A-Z]{1,2})([0-9])[- ]?([0-9]{3})(?:\\.([0-9]{2}))?\\b");
            var mm = motor.Match(all);
            if (mm.Success)
            {
                var right = (mm.Groups[5].Success ? mm.Groups[4].Value + mm.Groups[5].Value : mm.Groups[4].Value);
                return FormatPlateForFE(mm.Groups[1].Value, mm.Groups[2].Value, mm.Groups[3].Value, CleanNumericOCR(right));
            }

            var car = new Regex("\\b([0-9]{2})([A-Z]{1,2})([0-9])[- ]?([0-9]{5})\\b");
            var mc = car.Match(all);
            if (mc.Success)
            {
                return FormatPlateForFE(mc.Groups[1].Value, mc.Groups[2].Value, mc.Groups[3].Value, CleanNumericOCR(mc.Groups[4].Value));
            }

            return null;
        }

        private static string FormatPlateForFE(string province2, string alpha1or2, string series1, string digits)
        {
            var left = province2 + alpha1or2 + series1;
            var right = (digits ?? "").Replace(".", "");
            if (right.Length < 5)
            {
                if (int.TryParse(right, out var n)) right = n.ToString("D5");
            }
            else if (right.Length > 5) right = right[..5];
            return left + "-" + right;
        }

        private static string CleanNumericOCR(string s) => s?.Replace('O', '0').Replace('S', '5').Replace('B', '8') ?? "";

        private static bool IsValidVIN(string? vin) =>
            vin != null && Regex.IsMatch(vin.Trim().ToUpperInvariant(), "^[A-HJ-NPR-Z0-9]{17}$");

        private static bool IsValidPlate(string? plate) =>
            plate != null && Regex.IsMatch(plate.Replace(".", ""), "^[0-9]{2}[A-Z]{1,2}[0-9]{1}-[0-9]{5}$");

        private static string? NormalizePlate(string? raw)
        {
            if (raw == null) return null;
            var p = raw.ToUpperInvariant().Replace(" ", "").Replace(".", "");
            var m = Regex.Match(p, "^([0-9]{2}[A-Z]{1,2}[0-9]{1})([0-9]{5})$");
            if (m.Success) p = m.Groups[1].Value + "-" + m.Groups[2].Value;
            return p;
        }

        private static string? ExtractModel(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

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

            var normalized = text.ToLowerInvariant();
            foreach (var model in VALID_MODELS)
            {
                if (normalized.Contains(model.ToLowerInvariant()))
                    return model;
            }
            return null;
        }
    }
}
