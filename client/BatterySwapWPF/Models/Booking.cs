using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class Booking
{
    [JsonPropertyName("bookingId")]
    public int BookingId { get; set; }
    
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    
    [JsonPropertyName("vehicleId")]
    public int VehicleId { get; set; }
    
    [JsonPropertyName("packageId")]
    public int PackageId { get; set; }
    
    [JsonPropertyName("stationId")]
    public int? StationId { get; set; }
    
    [JsonPropertyName("chargingStationId")]
    public int? ChargingStationId { get; set; }
    
    [JsonPropertyName("slotId")]
    public int? SlotId { get; set; }
    
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
    
    [JsonPropertyName("vehicleLicense")]
    public string? VehicleLicense { get; set; }
    
    [JsonPropertyName("vehicleModel")]
    public string? VehicleModel { get; set; }
    
    [JsonPropertyName("packageName")]
    public string? PackageName { get; set; }
    
    [JsonPropertyName("stationName")]
    public string? StationName { get; set; }
    
    [JsonPropertyName("chargingStationName")]
    public string? ChargingStationName { get; set; }
    
    [JsonPropertyName("slotCode")]
    public string? SlotCode { get; set; }
    
    [JsonPropertyName("batteryModelRequested")]
    public string? BatteryModelRequested { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("bookingTime")]
    public string? BookingTime { get; set; }
    
    [JsonPropertyName("expiredDate")]
    public string? ExpiredDate { get; set; }
    
    [JsonPropertyName("qrCode")]
    public string? QrCode { get; set; }
}
