using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using webAPI.Data;
using webAPI.Models;
using webAPI.Utils;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    [Route("api/secure/booking")]  // giống @WebServlet("/api/secure/booking")
    [Authorize]
    public class BookingController : ControllerBase
    {
        private const int MAX_ACTIVE_BOOKINGS = 3;

        private readonly BookingRepository _bookingRepo;
        private readonly DriverPackageRepository _driverPkgRepo;
        private readonly BatterySlotRepository _slotRepo;
        private readonly VehicleRepository _vehicleRepo;
        private readonly string _connectionString;
        private readonly TimeZoneInfo _vnZone;

        // DTO nhận body từ FE
        public class BookingRequest
        {
            public string? stationName { get; set; }
            public string? station { get; set; }
            public string? bookingTime { get; set; }  // ISO hoặc dd/MM/yyyy hh:mm SA/CH
            public string? date { get; set; }         // fallback
            public string? time { get; set; }         // fallback
            public int? vehicleId { get; set; }
        }

        private static object Error(string msg) => new { error = msg };

        public BookingController(
            BookingRepository bookingRepo,
            DriverPackageRepository driverPkgRepo,
            BatterySlotRepository slotRepo,
            VehicleRepository vehicleRepo,
            IConfiguration config)
        {
            _bookingRepo = bookingRepo;
            _driverPkgRepo = driverPkgRepo;
            _slotRepo = slotRepo;
            _vehicleRepo = vehicleRepo;

            // dùng đúng tên connection string mà bạn cấu hình trong Program.cs
            _connectionString = config.GetConnectionString("BatterySwapDb")
                               ?? throw new InvalidOperationException("Missing connection string 'BatterySwapDb'");

            // Chuẩn múi giờ VN – hỗ trợ cả Linux & Windows
            try
            {
                _vnZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch
            {
                _vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] BookingRequest body)
        {
            // ===== 1. Lấy userId từ JWT =====
            var userIdStr = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(Error("Unauthorized: missing JWT user id"));
            }

            Console.WriteLine("[LOG] ==== New booking request ====");
            Console.WriteLine("[LOG] JWT user id = " + userId);

            // ===== 2. stationName / station =====
            var stationName = !string.IsNullOrWhiteSpace(body.stationName)
                ? body.stationName
                : body.station;

            if (string.IsNullOrWhiteSpace(stationName))
            {
                return BadRequest(Error("Missing 'stationName' (or 'station')"));
            }

            // ===== 3. vehicleId (bắt buộc) =====
            if (body.vehicleId is null)
            {
                return BadRequest(Error("Missing 'vehicleId'"));
            }
            var vehicleId = body.vehicleId.Value;

            // ===== 4. bookingTime =====
            var bookingTimeStr = body.bookingTime;
            if (string.IsNullOrWhiteSpace(bookingTimeStr))
            {
                if (!string.IsNullOrWhiteSpace(body.date) && !string.IsNullOrWhiteSpace(body.time))
                {
                    bookingTimeStr = $"{body.date}T{body.time}";
                }
            }
            if (string.IsNullOrWhiteSpace(bookingTimeStr))
            {
                return BadRequest(Error("Missing 'bookingTime' (or 'date' + 'time')"));
            }

            // Parse thời gian giống Java
            if (!TryParseBookingTime(bookingTimeStr, out var bookingLocal))
            {
                return BadRequest(Error("Invalid datetime format. Use ISO 'YYYY-MM-DDTHH:mm' or 'dd/MM/yyyy hh:mm SA/CH'"));
            }

            // ===== 5. Rule thời gian (theo múi giờ VN) =====
            var nowVn = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _vnZone);
            var todayVn = nowVn.Date;

            // chỉ trong hôm nay
            if (bookingLocal.Date != todayVn)
            {
                return BadRequest(Error(
                    $"Chỉ được đặt lịch trong HÔM NAY ({todayVn:yyyy-MM-dd}). Vui lòng chọn lại ngày."
                ));
            }

            // không được đặt trong quá khứ
            if (bookingLocal < nowVn)
            {
                var nowTime = nowVn.ToString("HH:mm");
                return BadRequest(Error(
                    $"Giờ đặt đã qua (hiện tại: {nowTime}). Vui lòng chọn giờ lớn hơn."
                ));
            }

            // Convert sang DateTime & set expired +1h
            var bookingTime = bookingLocal;
            var expiredTime = bookingLocal.AddHours(1);

            // ===== 6. Mở connection & xử lý nghiệp vụ =====
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();

            try
            {
                // 6.1 Station_ID
                var stationId = await _bookingRepo.GetStationIdByNameAsync(con, stationName);
                if (stationId == -1)
                {
                    return BadRequest(Error("Station not found"));
                }

                // 6.2 vehicle thuộc user
                var owns = await _vehicleRepo.UserOwnsVehicleAsync(userId, vehicleId);
                if (!owns)
                {
                    return StatusCode(403, Error("Vehicle does not belong to current user"));
                }

                // 6.3 Gói thuê hiện tại
                var pack = await _driverPkgRepo.GetCurrentPackageAsync(con, userId);
                if (pack == null)
                {
                    return BadRequest(Error("Bạn phải thuê gói pin mới đặt lịch trước!!!"));
                }

                // 6.4 Giới hạn MAX_ACTIVE_BOOKINGS
                var activeCount = await _bookingRepo.CountActiveBookingsAsync(con, userId, nowVn);
                if (activeCount >= MAX_ACTIVE_BOOKINGS)
                {
                    return StatusCode(409, Error(
                        $"Bạn đã đạt giới hạn {MAX_ACTIVE_BOOKINGS} lịch đặt trước đang hiệu lực. " +
                        "Vui lòng hoàn tất hoặc đợi hết hạn để đặt thêm."
                    ));
                }

                // 6.5 Battery type từ Vehicle_ID
                var batteryType = await _vehicleRepo.GetBatteryTypeByVehicleIdAsync(vehicleId);
                if (string.IsNullOrWhiteSpace(batteryType))
                {
                    return BadRequest(Error("Cannot resolve battery type for the selected vehicle"));
                }

                var modelFilter = batteryType.Trim();
                Console.WriteLine("[LOG] batteryType (from DB) = " + modelFilter);

                // 6.6 Tìm & giữ slot phù hợp
                var slot = await _slotRepo.FindAndReserveSuitableSlotAsync(
                    con,
                    stationId,
                    modelFilter,
                    pack.MinSoH,
                    pack.MaxSoH
                );

                if (slot == null)
                {
                    return NotFound(Error($"No suitable battery ({modelFilter}) found at this station"));
                }

                // 6.7 Tạo booking
                var booking = new Booking
                {
                    User_ID = userId,
                    Vehicle_ID = vehicleId,
                    Package_ID = pack.PackageId,
                    Station_ID = stationId,
                    ChargingStation_ID = slot.ChargingStation_ID,
                    Slot_ID = slot.Slot_ID,
                    Battery_Request = modelFilter,
                    Status = "Reserved",
                    Booking_Time = bookingTime,
                    Expired_Date = expiredTime,
                    Qr_Code = null
                };

                // 6.8 Insert booking
                var bookingId = await _bookingRepo.InsertBookingAsync(con, booking);
                if (bookingId <= 0)
                {
                    return StatusCode(500, Error("Không thể tạo đặt lịch trước!!!"));
                }

                // 6.9 Sinh QR code & update
                string? qr = null;
                try
                {
                    qr = QrCodeUtil.ToBase64Png($"BOOK-{bookingId}", 200);
                    if (!string.IsNullOrEmpty(qr))
                    {
                        await _bookingRepo.UpdateQRCodeAsync(con, bookingId, qr);
                    }
                }
                catch (Exception qrEx)
                {
                    Console.WriteLine(qrEx);
                    qr = null;
                }

                // 6.10 Thông tin xe để trả về "Model — Biển số"
                var vDetail = await _vehicleRepo.GetVehicleByIdAsync(vehicleId);
                string? vehicleModelName = vDetail?.Model_Name;
                string? licensePlate = vDetail?.License_Plate;

                string? vehicleLabel = null;
                if (!string.IsNullOrWhiteSpace(vehicleModelName) || !string.IsNullOrWhiteSpace(licensePlate))
                {
                    var modelNameSafe = !string.IsNullOrWhiteSpace(vehicleModelName) ? vehicleModelName : "Model ?";
                    var plateSafe = !string.IsNullOrWhiteSpace(licensePlate) ? licensePlate : "Biển số ?";
                    vehicleLabel = $"{modelNameSafe} — {plateSafe}";
                }

                // 6.11 Response JSON
                var resp = new
                {
                    bookingId,
                    stationId,
                    vehicleId,
                    vehicleLabel,
                    vehicleModelName,
                    licensePlate,
                    chargingStationId = slot.ChargingStation_ID,
                    slotId = slot.Slot_ID,
                    batteryType = modelFilter,
                    status = "Reserved",
                    bookingTime,
                    expiredTime,
                    qrCode = qr
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, Error("Internal server error: " + ex.Message));
            }
        }

        /// <summary>
        /// Parse bookingTime:
        /// - ISO: yyyy-MM-ddTHH:mm
        /// - "dd/MM/yyyy hh:mm SA/CH" (culture vi-VN)
        /// </summary>
        private static bool TryParseBookingTime(string input, out DateTime result)
        {
            if (DateTime.TryParseExact(
                    input,
                    "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
            {
                return true;
            }

            var vi = new CultureInfo("vi-VN");
            if (DateTime.TryParseExact(
                    input,
                    "dd/MM/yyyy hh:mm tt",
                    vi,
                    DateTimeStyles.None,
                    out result))
            {
                return true;
            }

            return false;
        }
    }
}
