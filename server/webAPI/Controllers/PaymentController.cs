using Microsoft.AspNetCore.Mvc;
using System.Text;
using webAPI.Config;
using webAPI.Services;

namespace webAPI.Controllers
{
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly PackageService _packageSvc;
        public PaymentController(PackageService packageSvc) => _packageSvc = packageSvc;

        // Lấy IP ưu tiên qua proxy/ngrok; ép IPv6/::1 về 127.0.0.1
        private string GetClientIp()
        {
            var xff = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(xff))
            {
                var first = xff.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    if (first == "::1" || first.Contains(":")) return "127.0.0.1";
                    return first;
                }
            }

            var xri = Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(xri))
            {
                if (xri == "::1" || xri.Contains(":")) return "127.0.0.1";
                return xri;
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ip == "::1" || ip.Contains(":")) return "127.0.0.1";
            return ip;
        }

        /// GET /api/payment?userId=1&packageId=3&orderType=buyPackage
        [HttpGet("api/payment")]
        public async Task<IActionResult> CreatePaymentUrl(
            [FromQuery] int userId,
            [FromQuery] int packageId,
            [FromQuery] string orderType = "buyPackage")
        {
            try
            {
                // 1) Validate
                if (userId <= 0 || packageId <= 0 || string.IsNullOrWhiteSpace(orderType))
                {
                    Response.StatusCode = 400;
                    return Content("Missing userId/packageId/orderType");
                }

                // 2) Lấy gói & verify active
                var pkg = await _packageSvc.GetActivePackageByIdAsync(packageId);
                if (pkg == null)
                {
                    Response.StatusCode = 400;
                    return Content("Package is not active or not found");
                }

                // 3) Amount VND * 100
                long amountVND = (long)Math.Round((decimal)pkg.Price);
                if (amountVND < 1000) amountVND = 1000;   // tránh reject sandbox vì quá nhỏ
                long amountForVNP = amountVND * 100;

                // 4) OrderInfo gửi PLAIN như Java
                string orderInfo = $"userId={userId}&packageId={packageId}&orderType={orderType}";

                // 5) Tham số chuẩn
                string vnp_TxnRef = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                string vnp_IpAddr = GetClientIp();
                var vnp = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["vnp_Version"] = "2.1.0",
                    ["vnp_Command"] = "pay",
                    ["vnp_TmnCode"] = VnPayConfig.vnp_TmnCode,
                    ["vnp_Amount"] = amountForVNP.ToString(),
                    ["vnp_CurrCode"] = "VND",
                    ["vnp_TxnRef"] = vnp_TxnRef,
                    ["vnp_OrderInfo"] = orderInfo,          // PLAIN
                    ["vnp_OrderType"] = orderType,
                    ["vnp_Locale"] = "vn",
                    ["vnp_ReturnUrl"] = VnPayConfig.vnp_ReturnUrl,
                    ["vnp_IpAddr"] = vnp_IpAddr,
                    ["vnp_CreateDate"] = VnPayUtil.VnTimeStringNow(),
                    ["vnp_ExpireDate"] = VnPayUtil.VnTimeStringAddMinutes(15)
                };

                // 6) Build hashData & query (encode kiểu Java)
                var sbHash = new StringBuilder();
                var sbQuery = new StringBuilder();
                int i = 0;
                foreach (var kv in vnp)
                {
                    var k = kv.Key;
                    var encV = VnPayUtil.UrlEncodeLikeJava(kv.Value);
                    var encK = VnPayUtil.UrlEncodeLikeJava(k);

                    if (i++ > 0) { sbHash.Append('&'); sbQuery.Append('&'); }
                    sbHash.Append(k).Append('=').Append(encV);     // hash: key=encoded(value)
                    sbQuery.Append(encK).Append('=').Append(encV); // query: encoded(key)=encoded(value)
                }

                // 7) Ký HMAC SHA512
                string vnp_SecureHash = VnPayUtil.HmacSHA512(VnPayConfig.vnp_HashSecret, sbHash.ToString());
                string paymentUrl = $"{VnPayConfig.vnp_PayUrl}?{sbQuery}&vnp_SecureHash={vnp_SecureHash}";

                // 8) Log debug
                Console.WriteLine("=== [VNPay] DEBUG ===");
                Console.WriteLine("[TmnCode]        " + VnPayConfig.vnp_TmnCode);
                Console.WriteLine("[ReturnUrl]      " + VnPayConfig.vnp_ReturnUrl);
                Console.WriteLine("[Amount VND]     " + amountVND);
                Console.WriteLine("[Amount *100]    " + amountForVNP);
                Console.WriteLine("[TxnRef]         " + vnp_TxnRef);
                Console.WriteLine("[Client IP]      " + vnp_IpAddr);
                Console.WriteLine("[CreateDate]     " + vnp["vnp_CreateDate"]);
                Console.WriteLine("[ExpireDate]     " + vnp["vnp_ExpireDate"]);
                Console.WriteLine("[OrderInfo]      " + orderInfo);
                Console.WriteLine("[HashData]       " + sbHash);
                Console.WriteLine("[SecureHash]     " + vnp_SecureHash);
                Console.WriteLine("[PaymentUrl]     " + paymentUrl);
                Console.WriteLine("=====================");

                // 9) Redirect
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VNPay] ERROR: " + ex);
                return Content($"Lỗi thanh toán: {ex.Message}");
            }
        }
    }
}
