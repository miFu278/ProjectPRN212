using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BatterySwapWPF.Helpers;
using BatterySwapWPF.Models;

namespace BatterySwapWPF.Services;

public class StationService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5187";

    public StationService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Station>?> GetAllStationsAsync()
    {
        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Assuming there's an API endpoint for stations
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/stations");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Station>>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
