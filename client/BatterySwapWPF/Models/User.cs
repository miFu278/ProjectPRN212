using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class User
{
    [JsonPropertyName("id")]
    public int ID { get; set; }
    
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("role")]
    public string? Role { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("stationId")]
    public int? Station_ID { get; set; }
    
    [JsonPropertyName("avatar_URL")]
    public string? Avatar_URL { get; set; }
}
