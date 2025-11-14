using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BatterySwapWPF.Helpers;
using BatterySwapWPF.Models;

namespace BatterySwapWPF.Services;

public class HistoryService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5187";

    public HistoryService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<SwapHistory>?> GetSwapHistoryAsync(string? from = null, string? to = null)
    {
        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"{BaseUrl}/api/secure/my-swaps";
            if (!string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to))
            {
                var query = new List<string>();
                if (!string.IsNullOrEmpty(from)) query.Add($"from={from}");
                if (!string.IsNullOrEmpty(to)) query.Add($"to={to}");
                url += "?" + string.Join("&", query);
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<SwapHistory>>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PackageHistory>?> GetPackageHistoryAsync(string? from = null, string? to = null)
    {
        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"{BaseUrl}/api/secure/my-packages";
            if (!string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to))
            {
                var query = new List<string>();
                if (!string.IsNullOrEmpty(from)) query.Add($"from={from}");
                if (!string.IsNullOrEmpty(to)) query.Add($"to={to}");
                url += "?" + string.Join("&", query);
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PackageHistory>>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
