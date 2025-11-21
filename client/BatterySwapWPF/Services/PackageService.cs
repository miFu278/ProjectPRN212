using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BatterySwapWPF.Helpers;
using BatterySwapWPF.Models;
using System.Net;

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

    public async Task<string?> GetPaymentUrlAsync(int userId, int packageId)
    {
        try
        {
            // Use handler that does not follow redirects so we can read Location header
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);

            var response = await client.GetAsync($"{BaseUrl}/api/payment?userId={userId}&packageId={packageId}&orderType=buyPackage");

            if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
            {
                return response.Headers.Location?.ToString();
            }

            // If not redirect, return response body for debugging
            var txt = await response.Content.ReadAsStringAsync();
            return null;
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
