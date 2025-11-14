using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BatterySwapWPF.Helpers;
using BatterySwapWPF.Models;

namespace BatterySwapWPF.Services;

public class BookingService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5187";

    public BookingService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Booking>?> GetMyBookingsAsync()
    {
        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{BaseUrl}/api/secure/GetBookings");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Booking>>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CreateBookingResponse?> CreateBookingAsync(CreateBookingRequest request)
    {
        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/secure/booking", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CreateBookingResponse>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

public class CreateBookingRequest
{
    public string? stationName { get; set; }
    public string? bookingTime { get; set; }
    public int vehicleId { get; set; }
}

public class CreateBookingResponse
{
    public int bookingId { get; set; }
    public int stationId { get; set; }
    public int vehicleId { get; set; }
    public string? vehicleLabel { get; set; }
    public int chargingStationId { get; set; }
    public int slotId { get; set; }
    public string? batteryType { get; set; }
    public string? status { get; set; }
    public DateTime bookingTime { get; set; }
    public DateTime expiredTime { get; set; }
    public string? qrCode { get; set; }
}
