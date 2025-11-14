using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BatterySwapWPF.Helpers;
using BatterySwapWPF.Models;

namespace BatterySwapWPF.Services;

public class PackageService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5187";

    public PackageService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Package>?> GetAllPackagesAsync()
    {
        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{BaseUrl}/api/secure/packages");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Package>>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
