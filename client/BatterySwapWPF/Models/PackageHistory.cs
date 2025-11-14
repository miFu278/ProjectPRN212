using System.Text.Json.Serialization;

namespace BatterySwapWPF.Models;

public class PackageHistory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    
    [JsonPropertyName("stationId")]
    public int? StationId { get; set; }
    
    [JsonPropertyName("packageId")]
    public int? PackageId { get; set; }
    
    [JsonPropertyName("packageName")]
    public string? PackageName { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("transactionTime")]
    public string? TransactionTime { get; set; }
}
