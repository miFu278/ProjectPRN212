using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class Vehicle
{
    [JsonPropertyName("vehicle_ID")]
    public int VehicleId { get; set; }

    [JsonPropertyName("license_Plate")]
    public string? LicensePlate { get; set; }

    [JsonPropertyName("model_Name")]
    public string? ModelName { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("battery_Type")]
    public string? BatteryType { get; set; }
}
