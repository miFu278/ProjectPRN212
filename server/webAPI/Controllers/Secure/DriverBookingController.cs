// Controllers/Secure/DriverBookingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using webAPI.Models;                    // EF entities
using webAPI.Models.BookingDtos;        // DTOs
using webAPI.Services.Ado;
using webAPI.Utils;
using BookingEntity = webAPI.Models.Booking;

namespace webAPI.Controllers.Secure
{
    [ApiController]
    public class DriverBookingController : ControllerBase
    {
        private static readonly TimeZoneInfo VN_TZ =
            TryGetTz("SE Asia Standard Time") ?? TryGetTz("Asia/Ho_Chi_Minh") ?? TimeZoneInfo.Local;
        private static TimeZoneInfo? TryGetTz(string id) { try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { return null; } }

        private const int MAX_ACTIVE_BOOKINGS = 3;

        private readonly BatterySwapContext _db;
        private readonly BookingAdo _booking;
        private readonly DriverPackageAdo _driverPkg;
        private readonly BatterySlotAdo _slot;
        private readonly VehicleAdo _vehicle;

        public DriverBookingController(
            BatterySwapContext db,
            BookingAdo booking,
            DriverPackageAdo driverPkg,
            BatterySlotAdo slot,
            VehicleAdo vehicle)
        {
            _db = db;
            _booking = booking;
            _driverPkg = driverPkg;
            _slot = slot;
            _vehicle = vehicle;
        }

        [Authorize]
        [HttpPost("api/secure/driver-booking")]
        public async Task<IActionResult> Create([FromBody] DriverCreateBookingRequest body, CancellationToken ct)
        {
            var sub = User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
            if (!int.TryParse(sub, out var userId))
                return Unauthorized(new { error = "Unauthorized: missing JWT user id" });

            var stationName = body.StationName;
            if (string.IsNullOrWhiteSpace(stationName))
                return BadRequest(new { error = "Missing 'stationName' (or 'station')" });

            if (body.VehicleId is null)
                return BadRequest(new { error = "Missing 'vehicleId'" });

            var bookingTimeStr = body.BookingTime;
            if (string.IsNullOrWhiteSpace(bookingTimeStr) && !string.IsNullOrWhiteSpace(body.Date) && !string.IsNullOrWhiteSpace(body.Time))
                bookingTimeStr = $"{body.Date}T{body.Time}";

            if (string.IsNullOrWhiteSpace(bookingTimeStr))
                return BadRequest(new { error = "Missing 'bookingTime' (or 'date' + 'time')" });

            if (!DateTime.TryParse(bookingTimeStr, out var bookingLocal))
                return BadRequest(new { error = "Invalid datetime format. Use ISO 'YYYY-MM-DDTHH:mm' or 'dd/MM/yyyy hh:mm SA/CH'" });

            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, VN_TZ);
            var todayLocal = nowLocal.Date;

            if (bookingLocal.Date != todayLocal)
                return BadRequest(new { error = $"Chỉ được đặt lịch trong HÔM NAY ({todayLocal:yyyy-MM-dd}). Vui lòng chọn lại ngày." });

            if (bookingLocal < nowLocal)
                return BadRequest(new { error = $"Giờ đặt đã qua (hiện tại: {nowLocal:HH:mm}). Vui lòng chọn giờ lớn hơn." });

            var expiredLocal = bookingLocal.AddHours(1);

            var conn = (SqlConnection)_db.Database.GetDbConnection();
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            try
            {
                var stationId = await _booking.GetStationIdByNameAsync(conn, (SqlTransaction)tx, stationName!, ct);
                if (stationId <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return BadRequest(new { error = "Station not found" });
                }

                var owns = await _vehicle.UserOwnsVehicleAsync(conn, (SqlTransaction)tx, userId, body.VehicleId.Value, ct);
                if (!owns)
                {
                    await tx.RollbackAsync(ct);
                    return StatusCode(403, new { error = "Vehicle does not belong to current user" });
                }

                var pkg = await _driverPkg.GetCurrentPackageAsync(conn, (SqlTransaction)tx, userId, ct);
                if (pkg == null)
                {
                    await tx.RollbackAsync(ct);
                    return BadRequest(new { error = "Bạn phải thuê gói pin mới đặt lịch trước!!!" });
                }
                var (packageId, minSoH, maxSoH) = pkg.Value;

                var active = await _booking.CountActiveBookingsAsync(conn, (SqlTransaction)tx, userId, nowLocal, ct);
                if (active >= MAX_ACTIVE_BOOKINGS)
                {
                    await tx.RollbackAsync(ct);
                    return StatusCode(409, new { error = $"Bạn đã đạt giới hạn {MAX_ACTIVE_BOOKINGS} lịch đặt trước đang hiệu lực. Vui lòng hoàn tất hoặc đợi hết hạn để đặt thêm." });
                }

                var batteryType = await _vehicle.GetBatteryTypeByVehicleIdAsync(conn, (SqlTransaction)tx, body.VehicleId.Value, ct);
                if (string.IsNullOrWhiteSpace(batteryType))
                {
                    await tx.RollbackAsync(ct);
                    return BadRequest(new { error = "Cannot resolve battery type for the selected vehicle" });
                }

                var picked = await _slot.FindAndReserveSuitableSlotAsync(conn, (SqlTransaction)tx,
                                stationId, batteryType!, minSoH, maxSoH, ct);
                if (picked == null)
                {
                    await tx.RollbackAsync(ct);
                    return NotFound(new { error = $"No suitable battery ({batteryType}) found at this station" });
                }

                var booking = new BookingEntity
                {
                    User_ID = userId,
                    Vehicle_ID = body.VehicleId.Value,
                    Package_ID = packageId,
                    Station_ID = stationId,
                    ChargingStation_ID = picked!.ChargingStationId, // <— sửa
                    Slot_ID = picked!.SlotId,                       // <— sửa
                    Battery_Request = batteryType,
                    Status = "Reserved",
                    Booking_Time = bookingLocal,
                    Expired_Date = expiredLocal,
                    Qr_Code = null
                };

                var bookingId = await _booking.InsertBookingAsync(conn, (SqlTransaction)tx, booking, ct);
                if (bookingId <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return StatusCode(500, new { error = "Không thể tạo đặt lịch trước!!!" });
                }

                var qrBase64 = QrCodeUtil.ToBase64Png($"BOOK-{bookingId}", 200);
                if (qrBase64 != null)
                {
                    await _booking.UpdateQRCodeAsync(conn, (SqlTransaction)tx, bookingId, qrBase64, ct);
                }

                var basic = await _booking.GetVehicleLabelAsync(conn, (SqlTransaction)tx, body.VehicleId.Value, ct);
                var vehicleLabel = (basic.ModelName ?? "Model ?") + " — " + (basic.Plate ?? "Biển số ?");

                await tx.CommitAsync(ct);

                return Ok(new DriverCreateBookingResponse
                {
                    BookingId = bookingId,
                    StationId = stationId,
                    VehicleId = body.VehicleId.Value,
                    VehicleLabel = vehicleLabel,
                    VehicleModelName = basic.ModelName,
                    LicensePlate = basic.Plate,
                    ChargingStationId = picked!.ChargingStationId, // <— sửa
                    SlotId = picked!.SlotId,                       // <— sửa
                    BatteryType = batteryType,
                    Status = "Reserved",
                    BookingTime = bookingLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                    ExpiredTime = expiredLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                    QrCode = qrBase64
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
