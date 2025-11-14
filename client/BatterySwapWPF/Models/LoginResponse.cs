namespace BatterySwapWPF.Models;

public class LoginResponse
{
    public string? Token { get; set; }
    public User? User { get; set; }
    public string? Message { get; set; }
}
