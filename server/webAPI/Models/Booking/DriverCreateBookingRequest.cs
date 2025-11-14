// Models/Booking/DriverCreateBookingRequest.cs
namespace webAPI.Models.BookingDtos
{
    public class DriverCreateBookingRequest
    {
        public string? StationName { get; set; }   // hoặc "Station"
        public int? VehicleId { get; set; }
        public string? BookingTime { get; set; }   // ISO: yyyy-MM-ddTHH:mm
        public string? Date { get; set; }          // optional fallback
        public string? Time { get; set; }          // optional fallback
    }

    public class DriverCreateBookingResponse
    {
        public int BookingId { get; set; }
        public int StationId { get; set; }
        public int VehicleId { get; set; }
        public string? VehicleLabel { get; set; }
        public string? VehicleModelName { get; set; }
        public string? LicensePlate { get; set; }
        public int ChargingStationId { get; set; }
        public int SlotId { get; set; }
        public string? BatteryType { get; set; }
        public string Status { get; set; } = "Reserved";
        public string BookingTime { get; set; } = "";
        public string ExpiredTime { get; set; } = "";
        public string? QrCode { get; set; }
    }
}
