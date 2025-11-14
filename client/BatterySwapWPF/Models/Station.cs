using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class Station
{
    [JsonPropertyName("station_ID")]
    public int StationId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}
