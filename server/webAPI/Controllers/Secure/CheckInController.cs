using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using webAPI.Models;   // Booking, Package, Battery, BatterySlot, SwapTransaction, PaymentTransaction, Users, Vehicle
using webAPI.Config;   // VnPayConfigSwap, VnPayUtil

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckInController : ControllerBase
    {
        private readonly BatterySwapContext _db;

        private readonly Random _rng = new();

        private const double NOMINAL_LI = 0.05;
        private const double NOMINAL_LFP = 0.03;

        public CheckInController(BatterySwapContext db)
        {
            _db = db;
        }

        /* ================= OPTIONS (preflight) ================= */
        [HttpOptions]
        public IActionResult Options()
        {
            SetCorsHeaders();
            return NoContent();
        }

        /* ================= GET: lấy info check-in / xử lý VNPay return ================= */
        [HttpGet]
        public IActionResult Get([FromQuery] int? bookingId)
        {
            SetCorsHeaders();

            bool isVnpReturn = Request.Query.Keys
                .Any(k => k != null && k.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase));

            if (!isVnpReturn)
            {
                // ===== CASE 1: FE lấy thông tin check-in =====
                var outJson = new Dictionary<string, object>();

                try
                {
                    if (bookingId == null || bookingId <= 0)
                    {
                        outJson["error"] = "Thiếu bookingId";
                        return Ok(outJson);
                    }

                    int id = bookingId.Value;

                    var booking = _db.Booking
                        .FirstOrDefault(b => b.Booking_ID == id);

                    if (booking == null)
                    {
                        outJson["error"] = "Booking không tồn tại";
                        return Ok(outJson);
                    }

                    // User (người đặt)
                    var user = _db.Users
                        .FirstOrDefault(u => u.ID == booking.User_ID);
                    string? bookerName = user?.FullName;

                    // Vehicle
                    var vehicle = _db.Vehicle
                        .FirstOrDefault(v => v.Vehicle_ID == booking.Vehicle_ID);
                    string? licensePlate = vehicle?.License_Plate;
                    string? vehicleModel = vehicle?.Model_Name;

                    // Slot đang giữ
                    int slotId = booking.Slot_ID;
                    BatterySlot? reservedSlot = null;
                    if (slotId > 0)
                    {
                        reservedSlot = _db.BatterySlot
                            .FirstOrDefault(s => s.Slot_ID == slotId);
                    }

                    // ƯỚC LƯỢNG SoH cũ (calendar aging)
                    double sohOld;
                    var lastSwap = _db.SwapTransaction
                        .Where(x => x.Driver_ID == booking.User_ID)
                        .OrderByDescending(x => x.Swap_Time)
                        .FirstOrDefault();

                    DateTime now = DateTime.Now;
                    if (lastSwap != null && lastSwap.SoH_New > 0 && lastSwap.Swap_Time != null)
                    {
                        DateTime swapTime = lastSwap.Swap_Time.Value;
                        long days = Math.Max(1, (long)(now - swapTime).TotalDays);
                        sohOld = Math.Max(50.0, (byte)(lastSwap.SoH_New - days * 0.2));  // giữ logic mô phỏng
                    }
                    else
                    {
                        sohOld = 65.0 + _rng.NextDouble() * (95.0 - 65.0);
                    }
                    sohOld = Math.Round(sohOld * 100.0) / 100.0;

                    // Slot object
                    var slotObj = new Dictionary<string, object?>();
                    if (reservedSlot != null)
                    {
                        slotObj["slotId"] = reservedSlot.Slot_ID;
                        slotObj["slotCode"] = reservedSlot.Slot_Code;
                        slotObj["chargingStationId"] = reservedSlot.ChargingStation_ID;
                        slotObj["chargingStationName"] = reservedSlot.ChargingStationName;
                        slotObj["chargingSlotType"] = reservedSlot.ChargingSlotType;
                        slotObj["batterySoHAtSlot"] = reservedSlot.BatterySoH;
                        slotObj["batterySerialAtSlot"] = reservedSlot.BatterySerial;
                    }

                    // Vehicle object
                    var vehicleObj = new Dictionary<string, object?>
                    {
                        ["vehicleId"] = booking.Vehicle_ID,
                        ["licensePlate"] = licensePlate,
                        ["modelName"] = vehicleModel
                    };

                    outJson["bookingId"] = booking.Booking_ID;
                    outJson["bookerName"] = bookerName ?? "";
                    outJson["vehicle"] = vehicleObj;
                    outJson["requestedBattery"] = booking.Battery_Request ?? "";
                    outJson["stationId"] = booking.Station_ID;
                    outJson["chargingStationId"] = booking.ChargingStation_ID;
                    outJson["slot"] = slotObj;
                    outJson["sohOldEstimate"] = sohOld;
                    outJson["status"] = booking.Status ?? "";
                    outJson["bookingTime"] = booking.Booking_Time;
                    outJson["expiredDate"] = booking.Expired_Date;

                    return Ok(outJson);
                }
                catch (Exception ex)
                {
                    outJson["error"] = "Lỗi lấy thông tin booking: " + ex.Message;
                    return Ok(outJson);
                }
            }

            // ===== CASE 2: return từ VNPay =====
            var vnpOut = new Dictionary<string, object>();

            try
            {
                if (!ValidateVnpReturnSignature())
                {
                    vnpOut["error"] = "Sai chữ ký VNPay";
                    return Ok(vnpOut);
                }

                string vnpResponseCode = Request.Query["vnp_ResponseCode"].ToString();
                string vnpAmountStr = Request.Query["vnp_Amount"].ToString();
                string vnpOrderInfo = Request.Query["vnp_OrderInfo"].ToString();

                string orderInfoRaw = vnpOrderInfo;
                if (!string.IsNullOrEmpty(orderInfoRaw) && orderInfoRaw.StartsWith("SWAP:"))
                    orderInfoRaw = orderInfoRaw["SWAP:".Length..];

                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(orderInfoRaw ?? ""));
                var ctx = ParseContext(decoded);

                int bookingIdCtx = SafeParseInt(ctx.GetValueOrDefault("b"), -1);
                int slotIdCtx = SafeParseInt(ctx.GetValueOrDefault("s"), -1);
                int newBatteryIdCtx = SafeParseInt(ctx.GetValueOrDefault("nb"), -1);
                double sohOldCtx = SafeParseDouble(ctx.GetValueOrDefault("so"), 0);
                double requiredSoHCtx = SafeParseDouble(ctx.GetValueOrDefault("rq"), 0);
                double expectedFee = SafeParseDouble(ctx.GetValueOrDefault("f"), 0);
                string model = ctx.GetValueOrDefault("m") ?? "Lithium-ion";
                int chargingStationIdCtx = SafeParseInt(ctx.GetValueOrDefault("cs"), -1);

                if (bookingIdCtx <= 0 || slotIdCtx <= 0 || newBatteryIdCtx <= 0)
                {
                    vnpOut["error"] = "Thiếu context giao dịch";
                    return Ok(vnpOut);
                }

                var booking2 = _db.Booking.FirstOrDefault(b => b.Booking_ID == bookingIdCtx);
                if (booking2 == null)
                {
                    vnpOut["error"] = "Booking không tồn tại";
                    return Ok(vnpOut);
                }

                if (!"Reserved".Equals(booking2.Status, StringComparison.OrdinalIgnoreCase) &&
                    !"AwaitingPayment".Equals(booking2.Status, StringComparison.OrdinalIgnoreCase))
                {
                    vnpOut["error"] = "Trạng thái booking không hợp lệ để hoàn tất thanh toán";
                    return Ok(vnpOut);
                }

                int driverId = booking2.User_ID;
                int stationId = booking2.Station_ID;

                // VNPay báo fail
                if (!"00".Equals(vnpResponseCode, StringComparison.OrdinalIgnoreCase))
                {
                    booking2.Status = "Reserved";
                    _db.SaveChanges();

                    vnpOut["status"] = "PaymentFailed";
                    vnpOut["message"] = $"Thanh toán thất bại hoặc bị huỷ (code={vnpResponseCode})";
                    return Ok(vnpOut);
                }

                // Thành công
                if (!long.TryParse(vnpAmountStr, out long vnpAmountLong))
                {
                    vnpOut["error"] = "Invalid amount format";
                    return Ok(vnpOut);
                }
                double paid = vnpAmountLong / 100.0;

                // Ghi PaymentTransaction
                var payment = new PaymentTransaction
                {
                    User_ID = driverId,
                    Station_ID = stationId,
                    Package_ID = null,
                    Amount = (decimal)paid,
                    Payment_Method = "VnPay",
                    Description = "Swap Battery",
                    Transaction_Time = DateTime.Now
                };
                _db.PaymentTransaction.Add(payment);
                _db.SaveChanges();

                int paymentId = payment.ID;   // EF Core tự gán Id

                // Check Slot + Battery
                var reservedSlot2 = _db.BatterySlot.FirstOrDefault(s => s.Slot_ID == slotIdCtx);
                if (reservedSlot2 == null ||
                    !"Reserved".Equals(reservedSlot2.State, StringComparison.OrdinalIgnoreCase))
                {
                    vnpOut["error"] = "Slot không còn khả dụng sau thanh toán";
                    return Ok(vnpOut);
                }

                var newBattery = _db.Battery.FirstOrDefault(b => b.Battery_ID == newBatteryIdCtx);
                if (newBattery == null || newBattery.Battery_ID == 0)
                {
                    vnpOut["error"] = "Pin mới không tồn tại sau thanh toán";
                    return Ok(vnpOut);
                }

                // Tạo battery cũ (pin khách trả lại)
                var oldBat = new Battery
                {
                    Serial_Number = "OLD-" + DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    SoH = sohOldCtx,
                    Resistance = CalcResistance(model, sohOldCtx),
                    Type_ID = ResolveBatteryTypeId(model)
                };
                _db.Battery.Add(oldBat);
                _db.SaveChanges();

                int oldBatteryId = oldBat.Battery_ID;

                // Swap trong DB: slot chứa pin cũ, pin mới bị xóa
                reservedSlot2.Battery_ID = oldBatteryId;
                reservedSlot2.Condition = (sohOldCtx >= 70) ? "Weak" : "Damage";

                // Ghi SwapTransaction
                var tx = new SwapTransaction
                {
                    Driver_ID = driverId,
                    Staff_ID = null,
                    Station_ID = stationId,
                    ChargingStation_ID = chargingStationIdCtx,
                    Old_Battery = oldBatteryId,
                    New_Battery = newBatteryIdCtx,
                    SoH_Old = sohOldCtx,
                    SoH_New = newBattery.SoH,
                    Fee = (decimal?)paid,
                    Payment_ID = paymentId,
                    Status = "Completed",
                    Swap_Time = DateTime.Now,
                    Booking_ID = bookingIdCtx
                };
                _db.SwapTransaction.Add(tx);

                // Cập nhật booking
                booking2.Status = "Completed";

                // Lưu tất cả thay đổi (slot + payment + swap + booking)
                _db.SaveChanges();

                // XÓA thẳng pin mới ra khỏi DB (giống code Java deleteBattery)
                _db.Database.ExecuteSqlRaw(
                    "DELETE FROM dbo.Battery WHERE Battery_ID = {0}",
                    newBatteryIdCtx
                );

                vnpOut["status"] = "PaymentSuccess";
                vnpOut["transactionId"] = tx.ID;
                vnpOut["paymentId"] = paymentId;
                vnpOut["message"] = "Đổi pin thành công sau khi thanh toán";

                return Ok(vnpOut);
            }
            catch (Exception ex)
            {
                vnpOut["error"] = "Lỗi xử lý VNPay: " + ex.Message;
                return Ok(vnpOut);
            }
        }

        /* ================= POST: check-in tại trạm ================= */
        [HttpPost]
        public IActionResult Post([FromForm(Name = "bookingId")] int bookingId)
        {
            SetCorsHeaders();

            try
            {
                if (bookingId <= 0)
                    return Ok(Error("Thiếu hoặc sai bookingId"));

                var booking = _db.Booking.FirstOrDefault(b => b.Booking_ID == bookingId);
                if (booking == null)
                    return Ok(Error("Booking không tồn tại"));

                if (!"Reserved".Equals(booking.Status, StringComparison.OrdinalIgnoreCase))
                    return Ok(Error("Booking không hợp lệ hoặc đã check-in"));

                if (booking.Expired_Date != null && booking.Expired_Date < DateTime.Now)
                    return Ok(Error("Booking đã hết hạn"));

                int driverId = booking.User_ID;
                int stationId = booking.Station_ID;
                int chargingStationId = booking.ChargingStation_ID;
                string modelRequested = booking.Battery_Request ?? "Lithium-ion";
                int packageId = booking.Package_ID;
                int slotId = booking.Slot_ID;

                // Package
                var pkg = _db.Package.FirstOrDefault(p => p.Package_ID == packageId);
                if (pkg == null)
                    return Ok(Error("Package không tồn tại"));

                double requiredSoH = pkg.RequiredSoH;

                // Slot
                var reservedSlot = _db.BatterySlot.FirstOrDefault(s => s.Slot_ID == slotId);
                if (reservedSlot == null ||
                    !"Reserved".Equals(reservedSlot.State, StringComparison.OrdinalIgnoreCase) ||
                    !"Good".Equals(reservedSlot.Condition, StringComparison.OrdinalIgnoreCase))
                    return Ok(Error("Slot không khả dụng"));

                // Battery new
                var newBattery = _db.Battery.FirstOrDefault(b => b.Battery_ID == reservedSlot.Battery_ID);
                if (newBattery == null || newBattery.Battery_ID == 0)
                    return Ok(Error("Pin mới không tồn tại hoặc chưa có ID"));

                int newBatteryId = newBattery.Battery_ID;
                double newBatterySoH = newBattery.SoH ?? 0;

                string? newBatterySerial = newBattery.Serial_Number;
                int? newBatteryTypeId = newBattery.Type_ID;
                string newBatteryModel = NormalizeModel(modelRequested);

                string? slotCode = reservedSlot.Slot_Code;
                string? chargingStationName = reservedSlot.ChargingStationName;
                string? chargingSlotType = reservedSlot.ChargingSlotType;

                // Ước lượng SoH cũ
                var lastSwap = _db.SwapTransaction
                    .Where(x => x.Driver_ID == driverId)
                    .OrderByDescending(x => x.Swap_Time)
                    .FirstOrDefault();

                double sohOld;
                DateTime now = DateTime.Now;
                if (lastSwap != null && lastSwap.SoH_New > 0 && lastSwap.Swap_Time != null)
                {
                    DateTime lastTime = lastSwap.Swap_Time.Value;
                    long days = Math.Max(1, (long)(now - lastTime).TotalDays);
                    sohOld = Math.Max(50.0, (byte)(lastSwap.SoH_New - days * 0.2));
                }
                else
                {
                    sohOld = 65.0 + _rng.NextDouble() * (95.0 - 65.0);
                }
                sohOld = Math.Round(sohOld * 100.0) / 100.0;

                // Tính phí
                double fee = Math.Max(0, Math.Round((requiredSoH - sohOld) * 20000.0));
                bool free = (fee == 0);

                var result = new Dictionary<string, object>
                {
                    ["bookingId"] = bookingId,
                    ["driverId"] = driverId,
                    ["stationId"] = stationId,
                    ["chargingStationId"] = chargingStationId,
                    ["sohOld"] = sohOld,
                    ["requiredSoH"] = requiredSoH,
                    ["fee"] = fee,
                    ["newBattery"] = new Dictionary<string, object?>
                    {
                        ["batteryId"] = newBatteryId,
                        ["soh"] = newBatterySoH,
                        ["serial"] = newBatterySerial,
                        ["model"] = newBatteryModel,
                        ["typeId"] = newBatteryTypeId
                    },
                    ["slot"] = new Dictionary<string, object?>
                    {
                        ["slotId"] = slotId,
                        ["slotCode"] = slotCode,
                        ["chargingStationId"] = chargingStationId,
                        ["chargingStationName"] = chargingStationName,
                        ["chargingSlotType"] = chargingSlotType
                    }
                };

                // fee > 0 → tạo VNPay URL
                if (!free)
                {
                    string txnRef = $"SWAP-{bookingId}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

                    string contextRaw = string.Format(
                        CultureInfo.InvariantCulture,
                        "b={0}|s={1}|nb={2}|so={3:F2}|rq={4:F2}|f={5:F0}|m={6}|cs={7}",
                        bookingId, slotId, newBatteryId, sohOld, requiredSoH, fee,
                        NormalizeModel(modelRequested), chargingStationId
                    );
                    string base64Context = Convert.ToBase64String(Encoding.UTF8.GetBytes(contextRaw));
                    string orderInfo = "SWAP:" + base64Context;

                    string paymentUrl = VnPayUtil.CreateSwapPaymentUrl(HttpContext, txnRef, (long)fee, orderInfo);

                    booking.Status = "AwaitingPayment";
                    _db.SaveChanges();

                    result["txnRef"] = txnRef;
                    result["paymentUrl"] = paymentUrl;
                    result["message"] = "Cần thanh toán trước khi đổi pin";

                    return Ok(result);
                }

                // free → swap ngay + payment 0đ
                var oldBat = new Battery
                {
                    Serial_Number = "OLD-" + DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    SoH = sohOld,
                    Resistance = CalcResistance(modelRequested, sohOld),
                    Type_ID = ResolveBatteryTypeId(modelRequested)
                };
                _db.Battery.Add(oldBat);
                _db.SaveChanges();
                int oldBatteryId = oldBat.Battery_ID;

                reservedSlot.Battery_ID = oldBatteryId;
                reservedSlot.Condition = (sohOld >= 70) ? "Weak" : "Damage";

                var payment0 = new PaymentTransaction
                {
                    User_ID = driverId,
                    Station_ID = stationId,
                    Package_ID = null,
                    Amount = 0,
                    Payment_Method = "Cash",
                    Description = "Swap Battery (Free)",
                    Transaction_Time = DateTime.Now
                };
                _db.PaymentTransaction.Add(payment0);
                _db.SaveChanges();

                int paymentId0 = payment0.ID;

                var txFree = new SwapTransaction
                {
                    Driver_ID = driverId,
                    Staff_ID = null,
                    Station_ID = stationId,
                    ChargingStation_ID = chargingStationId,
                    Old_Battery = oldBatteryId,
                    New_Battery = newBatteryId,
                    SoH_Old = sohOld,
                    SoH_New = newBatterySoH,
                    Fee = 0,
                    Payment_ID = paymentId0,
                    Status = "Completed",
                    Swap_Time = DateTime.Now,
                    Booking_ID = bookingId
                };
                _db.SwapTransaction.Add(txFree);

                booking.Status = "Completed";
                _db.SaveChanges();

                // XÓA thẳng pin mới ra khỏi DB (giống Java)
                _db.Database.ExecuteSqlRaw(
                    "DELETE FROM dbo.Battery WHERE Battery_ID = {0}",
                    newBatteryId
                );

                result["message"] = "Đổi pin miễn phí";
                result["transactionId"] = txFree.ID;
                result["paymentId"] = paymentId0;
                result["oldBatteryId"] = oldBatteryId;
                result["newBatteryId"] = newBatteryId;
                result["sohNew"] = newBatterySoH;

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(Error("Lỗi xử lý: " + ex.Message));
            }
        }

        /* ================= Helpers ================= */

        private static Dictionary<string, object> Error(string msg)
            => new() { ["error"] = msg };

        private static string NormalizeModel(string? modelRequested)
        {
            if (modelRequested == null) return "Lithium-ion";
            string s = modelRequested.ToLowerInvariant();
            if (s.Contains("lfp")) return "LFP";
            if (s.Contains("lithium")) return "Lithium-ion";
            return "Lithium-ion";
        }

        private double CalcResistance(string model, double sohOld)
        {
            double nominal = NormalizeModel(model) == "LFP" ? NOMINAL_LFP : NOMINAL_LI;
            if (sohOld > 0)
            {
                double value = nominal * 100.0 / sohOld;
                return Math.Round(value * 1_000_000.0) / 1_000_000.0;
            }
            return nominal;
        }

        private int ResolveBatteryTypeId(string model)
            => NormalizeModel(model) == "LFP" ? 2 : 1;

        private static Dictionary<string, string> ParseContext(string s)
        {
            var map = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(s)) return map;

            foreach (var p in s.Split('|'))
            {
                int i = p.IndexOf('=');
                if (i > 0 && i < p.Length - 1)
                {
                    string key = p[..i].Trim();
                    string val = p[(i + 1)..].Trim();
                    map[key] = val;
                }
            }
            return map;
        }

        private static int SafeParseInt(string? s, int defVal)
            => int.TryParse(s, out var v) ? v : defVal;

        private static double SafeParseDouble(string? s, double defVal)
        {
            if (string.IsNullOrWhiteSpace(s)) return defVal;
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : defVal;
        }

        private void SetCorsHeaders()
        {
            var origin = Request.Headers.Origin.ToString();
            bool allowed = origin == "http://localhost:5173" || origin == "http://127.0.0.1:5173";

            if (allowed)
            {
                Response.Headers.AccessControlAllowOrigin = origin;
                Response.Headers.AccessControlAllowCredentials = "true";
            }

            Response.Headers.Vary = "Origin";
            Response.Headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
            Response.Headers.AccessControlAllowHeaders = "Content-Type, Accept, Authorization";
            Response.Headers.AccessControlExposeHeaders = "Authorization";
            Response.Headers.AccessControlMaxAge = "86400";
        }

        private bool ValidateVnpReturnSignature()
        {
            try
            {
                string secureHash = Request.Query["vnp_SecureHash"].ToString();
                if (string.IsNullOrEmpty(secureHash)) return false;

                var fields = new Dictionary<string, string>();
                foreach (var kv in Request.Query)
                {
                    string k = kv.Key;
                    string v = kv.Value.ToString();
                    if (string.IsNullOrEmpty(k) || string.IsNullOrEmpty(v)) continue;
                    if (k.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                        !k.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
                    {
                        fields[k] = v;
                    }
                }

                var keys = fields.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
                var sb = new StringBuilder();
                for (int i = 0; i < keys.Count; i++)
                {
                    string k = keys[i];
                    string v = fields[k];
                    sb.Append(k).Append('=').Append(WebUtility.UrlEncode(v));
                    if (i < keys.Count - 1) sb.Append('&');
                }

                string data = sb.ToString();
                string secret = VnPayConfigSwap.vnp_HashSecret;
                string myHash = HmacSHA512(secret, data);
                return secureHash.Equals(myHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
