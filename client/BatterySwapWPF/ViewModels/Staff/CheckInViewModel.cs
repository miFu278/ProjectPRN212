using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Net.Http.Headers;
using BatterySwapWPF.Helpers;
 

namespace BatterySwapWPF.ViewModels.Staff;

public partial class CheckInViewModel : ObservableObject
{
    [ObservableProperty]
    private string? bookingId;

    [ObservableProperty]
    private string? successMessage;

    [ObservableProperty]
    private string? errorMessage;

    // Booking details display removed per user request

    [RelayCommand]
    private async Task CheckInAsync()
    {
        SuccessMessage = null;
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(BookingId))
        {
            ErrorMessage = "Please enter a booking ID";
            return;
        }

        try
        {
            var token = SecureStorage.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Not authenticated";
                return;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("bookingId", BookingId!)
            });

            var response = await httpClient.PostAsync($"http://localhost:5187/api/CheckIn", content);

            var responseText = await response.Content.ReadAsStringAsync();

            // The server sometimes returns HTTP 200 with an error object { "error": "..." }.
            // Treat such responses as failures by inspecting the JSON body.
            if (response.IsSuccessStatusCode)
            {
                bool hasError = false;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(responseText);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("error", out var err))
                        {
                            ErrorMessage = err.GetString() ?? "Check-in failed";
                            hasError = true;
                        }
                        else if (doc.RootElement.TryGetProperty("status", out var statusProp))
                        {
                            var st = statusProp.GetString();
                            if (!string.IsNullOrEmpty(st) && st.Equals("fail", StringComparison.OrdinalIgnoreCase))
                            {
                                // Try to get message or error field
                                if (doc.RootElement.TryGetProperty("message", out var msg))
                                    ErrorMessage = msg.GetString();
                                else if (doc.RootElement.TryGetProperty("error", out var err2))
                                    ErrorMessage = err2.GetString();
                                else
                                    ErrorMessage = "Check-in failed (server returned fail).";
                                hasError = true;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore JSON parse errors, fall back to plain text checks
                }

                if (hasError)
                {
                    return;
                }

                // No error field found - treat as success
                SuccessMessage = $"Check-in successful for booking #{BookingId}";
                BookingId = string.Empty;
            }
            else
            {
                ErrorMessage = $"Check-in failed: {responseText}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    // Booking details loader removed; UI no longer shows booking info
}
