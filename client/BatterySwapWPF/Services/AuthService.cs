using System.Net.Http;
using System.Net.Http.Json;
using BatterySwapWPF.Models;

namespace BatterySwapWPF.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5187";

    public AuthService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new { email, password };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/login", request);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiLoginResponse>();

                if (apiResponse?.Status == "success" && apiResponse.Token != null)
                {
                    return new LoginResponse
                    {
                        Token = apiResponse.Token,
                        User = apiResponse.User
                    };
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private class ApiLoginResponse
    {
        public string? Status { get; set; }
        public string? Token { get; set; }
        public User? User { get; set; }
    }
}
