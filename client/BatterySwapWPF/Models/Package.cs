using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class Package
{
    [JsonPropertyName("package_ID")]
    public int PackageId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("required_SoH")]
    public decimal? RequiredSoH { get; set; }
    
    [JsonPropertyName("minSoH")]
    public int MinSoH { get; set; }
    
    [JsonPropertyName("maxSoH")]
    public int MaxSoH { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
