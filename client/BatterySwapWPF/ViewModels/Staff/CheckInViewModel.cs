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

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = $"Check-in successful for booking #{BookingId}";
                BookingId = string.Empty;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Check-in failed: {error}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }
}
