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

    /// <summary>
    /// Get station list from API. If <paramref name="requireAuth"/> is true,
    /// the method will attach saved JWT token from <see cref="SecureStorage"/>.
    /// </summary>
    public async Task<List<Station>?> GetStationsAsync(bool activeOnly = true, bool requireAuth = false)
    {
        try
        {
            var url = $"{BaseUrl}/api/stations" + (activeOnly ? "?activeOnly=true" : string.Empty);

            if (requireAuth)
            {
                var token = SecureStorage.GetToken();
                if (string.IsNullOrEmpty(token))
                    return null;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.GetAsync(url);
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
