using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class SwapHistory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("driverId")]
    public int DriverId { get; set; }
    
    [JsonPropertyName("stationId")]
    public int StationId { get; set; }
    
    [JsonPropertyName("stationName")]
    public string? StationName { get; set; }
    
    [JsonPropertyName("oldBattery")]
    public int? OldBattery { get; set; }
    
    [JsonPropertyName("newBattery")]
    public int? NewBattery { get; set; }
    
    [JsonPropertyName("soH_Old")]
    public double? SoH_Old { get; set; }
    
    [JsonPropertyName("soH_New")]
    public double? SoH_New { get; set; }
    
    [JsonPropertyName("fee")]
    public decimal? Fee { get; set; }
    
    [JsonPropertyName("paymentId")]
    public int? PaymentId { get; set; }
    
    [JsonPropertyName("bookingId")]
    public int? BookingId { get; set; }
    
    [JsonPropertyName("chargingStationId")]
    public int? ChargingStationId { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("swapTime")]
    public string? SwapTime { get; set; }
}
