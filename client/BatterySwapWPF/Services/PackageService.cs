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

    // Call public endpoint /api/getpackages which returns wrapper { status, data }
    public async Task<List<Package>?> GetPublicPackagesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/getpackages");
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<PublicPackagesResponse>();
            return wrapper?.data;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool?> CheckUserHasPackageAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/checkpackage?userId={userId}");
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<CheckPackageResponse>();
            return wrapper?.data;
        }
        catch
        {
            return null;
        }

    }

}

// Local DTOs to parse public API responses
internal class PublicPackagesResponse
{
    public string? status { get; set; }
    public List<Package>? data { get; set; }
}

internal class CheckPackageResponse
{
    public string? status { get; set; }
    public bool data { get; set; }
}
