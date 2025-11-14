using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using webAPI.Config;
using webAPI.Models;              // BatterySwapContext
using webAPI.Services;

namespace webAPI.Controllers
{
    [ApiController]
    public class BuyPackageController : ControllerBase
    {
        private readonly PackageService _pkgSvc;
        private readonly DriverPackageService _driverPkgSvc;
        private readonly PaymentTransactionService _paymentSvc;
        private readonly BatterySwapContext _db;          // để log ConnectionString
        private readonly ILogger<BuyPackageController> _logger;

        public BuyPackageController(
            PackageService pkgSvc,
            DriverPackageService driverPkgSvc,
            PaymentTransactionService paymentSvc,
            BatterySwapContext db,
            ILogger<BuyPackageController> logger)
        {
            _pkgSvc = pkgSvc;
            _driverPkgSvc = driverPkgSvc;
            _paymentSvc = paymentSvc;
            _db = db;
            _logger = logger;
        }

        /// Return URL: GET /api/buyPackage?... (VNPay gọi về)
        /// - Verify chữ ký từ GIÁ TRỊ ĐÃ ENCODE trong raw query (không re-encode).
        /// - vnp_OrderInfo dùng PLAIN: "userId=1&packageId=2&orderType=buyPackage".
        /// - Log ConnLen để bắt lỗi mất connection string.
        [HttpGet("api/buyPackage")]
        [Produces("text/plain")]
        public async Task<IActionResult> Get()
        {
            Response.ContentType = "text/plain; charset=utf-8";

            try
            {
                // 0) Check connection string runtime
                var conn = _db.Database.GetDbConnection().ConnectionString ?? "";
                if (string.IsNullOrWhiteSpace(conn))
                    return Content("❌ DB connection string is EMPTY at callback. Kiểm tra Program.cs & appsettings (key 'BatterySwapDb').");
                _logger.LogInformation("[BuyPackage] ConnLen={len}", conn.Length);

                // 1) Lấy raw query string giữ nguyên encode
                var rawQs = Request.QueryString.HasValue ? Request.QueryString.Value! : string.Empty;
                if (string.IsNullOrEmpty(rawQs) || rawQs.Length <= 1)
                    return Content("⚠️ Thiếu tham số VNPay.");

                var encodedMap = new Dictionary<string, string>(StringComparer.Ordinal);
                string? vnp_SecureHash = null;

                foreach (var pair in rawQs.Substring(1).Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = pair.IndexOf('=');
                    if (idx <= 0) continue;

                    var k = pair.Substring(0, idx);
                    var v = pair.Substring(idx + 1); // giữ nguyên encoded

                    if (k.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
                    {
                        vnp_SecureHash = v;
                        continue;
                    }
                    if (k.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                        continue;

                    encodedMap[k] = v;
                }

                if (string.IsNullOrEmpty(vnp_SecureHash))
                    return Content("⚠️ Thiếu vnp_SecureHash.");

                // 2) Ký lại từ encoded values (sort Ordinal)
                var sortedKeys = encodedMap.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
                var sb = new StringBuilder();
                for (int i = 0; i < sortedKeys.Count; i++)
                {
                    var k = sortedKeys[i];
                    var v = encodedMap[k]; // encoded
                    sb.Append(k).Append('=').Append(v);
                    if (i < sortedKeys.Count - 1) sb.Append('&');
                }
                var hashData = sb.ToString();
                var signValue = VnPayUtil.HmacSHA512(VnPayConfig.vnp_HashSecret, hashData);

                if (!signValue.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase))
                    return Content("❌ Chữ ký không hợp lệ!");

                // 3) Kiểm tra trạng thái thanh toán
                var responseCode = Request.Query["vnp_ResponseCode"].ToString();
                if (!string.Equals(responseCode, "00", StringComparison.Ordinal))
                    return Content($"❌ Thanh toán thất bại! Mã lỗi: {responseCode}");

                // 4) Parse OrderInfo (PLAIN)
                var orderInfoPlain = Request.Query["vnp_OrderInfo"].ToString();
                var infoMap = ParseOrderInfo(orderInfoPlain);

                if (!int.TryParse(infoMap.GetValueOrDefault("userId"), out var userId) ||
                    !int.TryParse(infoMap.GetValueOrDefault("packageId"), out var packageId))
                {
                    return Content("⚠️ Thiếu thông tin userId/packageId trong OrderInfo");
                }

                // 5) Nghiệp vụ: kiểm tra gói & cập nhật DB
                var pkg = await _pkgSvc.GetActivePackageByIdAsync(packageId);
                if (pkg == null) return Content("⚠️ Gói đã bị vô hiệu hóa hoặc không tồn tại.");

                var start = DateTime.Today;
                var end = start.AddDays(30);

                var exists = await _driverPkgSvc.ExistsAsync(userId);
                bool ok = exists
                    ? await _driverPkgSvc.UpdateAsync(userId, packageId, start, end)
                    : await _driverPkgSvc.InsertAsync(userId, packageId, start, end);

                if (!ok) return Content("⚠️ Thanh toán thành công nhưng lưu DB thất bại!");

                var amountStr = Request.Query["vnp_Amount"].ToString();
                var amount = double.TryParse(amountStr, out var raw) ? raw / 100.0 : 0.0;

                await _paymentSvc.InsertPaymentAsync(
                    userId: userId,
                    stationId: null,
                    packageId: packageId,
                    amountDouble: amount,
                    method: "VNPay",
                    description: "Buy Battery Package",
                    at: DateTime.Now
                );

                return Content(exists
                    ? $"🔄 Cập nhật thành công! Gói {packageId} cho User {userId}"
                    : $"✅ Thanh toán thành công! Gói {packageId} cho User {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BuyPackage] Exception");
                return Content("⚠️ Lỗi xử lý thanh toán: " + ex.Message);
            }
        }

        // Parse "userId=1&packageId=2&orderType=buyPackage" (PLAIN)
        private static Dictionary<string, string> ParseOrderInfo(string orderInfo)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(orderInfo)) return map;

            foreach (var pair in orderInfo.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx > 0 && idx < pair.Length - 1)
                {
                    var k = pair[..idx];
                    var v = pair[(idx + 1)..];
                    map[k] = v;
                }
            }
            return map;
        }
    }
}
