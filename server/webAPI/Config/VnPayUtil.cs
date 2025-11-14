using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace webAPI.Config
{
    public static class VnPayUtil
    {
        /* ================= HMAC SHA512 (giống Java) ================= */
        public static string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /* =============== Thời gian VN (GMT+7) dạng yyyyMMddHHmmss =============== */

        public static string VnTimeStringNow()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            return now.ToString("yyyyMMddHHmmss");
        }

        public static string VnTimeStringAddMinutes(int minutes)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var t = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz).AddMinutes(minutes);
            return t.ToString("yyyyMMddHHmmss");
        }

        /* =============== Encode giống Java URLEncoder (space -> '+') =============== */
        public static string UrlEncodeLikeJava(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var enc = WebUtility.UrlEncode(value ?? string.Empty);
            return enc?.Replace("%20", "+") ?? string.Empty;
        }

        /* ================== Lấy IP client (fix lỗi GetClientIp) ================== */
        private static string GetClientIp(HttpContext context)
        {
            // giống getHeader("X-Forwarded-For") bên Java
            string? ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                int comma = ip.IndexOf(',');
                return (comma > -1 ? ip[..comma] : ip).Trim();
            }

            ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
                return ip;

            return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        /* ================== Tạo URL thanh toán cho CHECK-IN / SWAP ================== */
        // orderInfo: đã được Base64 từ CheckInController (giống Java)
        public static string CreateSwapPaymentUrl(
            HttpContext httpContext,
            string txnRef,
            long amountVnd,
            string orderInfo)
        {
            string vnp_TmnCode = VnPayConfigSwap.vnp_TmnCode;
            string vnp_HashSecret = VnPayConfigSwap.vnp_HashSecret;
            string vnp_Url = VnPayConfigSwap.vnp_PayUrl;
            string vnp_ReturnUrl = VnPayConfigSwap.vnp_ReturnUrl;

            // VNPay yêu cầu amount * 100
            string vnp_Amount = (amountVnd * 100L).ToString();

            // Thời gian GMT+7
            string createDate = VnTimeStringNow();
            string expireDate = VnTimeStringAddMinutes(15);

            var vnp_Params = new Dictionary<string, string?>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = vnp_TmnCode,
                ["vnp_Amount"] = vnp_Amount,
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = orderInfo,     // đã Base64
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = vnp_ReturnUrl,
                ["vnp_CreateDate"] = createDate,
                ["vnp_ExpireDate"] = expireDate,
                ["vnp_IpAddr"] = GetClientIp(httpContext)
            };

            // sort key
            var fieldNames = vnp_Params.Keys.ToList();
            fieldNames.Sort(StringComparer.Ordinal);

            var hashData = new StringBuilder();
            var query = new StringBuilder();

            for (int i = 0; i < fieldNames.Count; i++)
            {
                string key = fieldNames[i];
                string? value = vnp_Params[key];

                if (string.IsNullOrEmpty(value))
                    continue;

                // URLEncoder bên Java: UTF-8 + space -> '+'
                string encValue = UrlEncodeLikeJava(value);

                hashData.Append(key).Append("=").Append(encValue);
                query.Append(key).Append("=").Append(encValue);

                if (i < fieldNames.Count - 1)
                {
                    hashData.Append("&");
                    query.Append("&");
                }
            }

            // Tính vnp_SecureHash
            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, hashData.ToString());
            query.Append("&vnp_SecureHash=").Append(vnp_SecureHash);

            return $"{vnp_Url}?{query}";
        }
    }
}
