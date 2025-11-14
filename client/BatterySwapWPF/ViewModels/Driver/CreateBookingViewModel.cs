using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System.Collections.ObjectModel;

namespace BatterySwapWPF.ViewModels.Driver;

public partial class CreateBookingViewModel : ObservableObject
{
    private readonly VehicleService _vehicleService;
    private readonly BookingService _bookingService;

    [ObservableProperty]
    private ObservableCollection<Vehicle> vehicles = new();

    [ObservableProperty]
    private Vehicle? selectedVehicle;

    [ObservableProperty]
    private string? stationName;

    [ObservableProperty]
    private DateTime? bookingDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan? bookingTime = DateTime.Now.TimeOfDay;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? successMessage;

    [ObservableProperty]
    private string? errorMessage;

    public CreateBookingViewModel()
    {
        _vehicleService = new VehicleService();
        _bookingService = new BookingService();
        _ = LoadVehiclesAsync();
    }

    private async Task LoadVehiclesAsync()
    {
        try
        {
            var result = await _vehicleService.GetMyVehiclesAsync();
            if (result != null)
            {
                Vehicles.Clear();
                foreach (var vehicle in result)
                    Vehicles.Add(vehicle);
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task CreateBookingAsync()
    {
        SuccessMessage = null;
        ErrorMessage = null;

        if (SelectedVehicle == null)
        {
            ErrorMessage = "Please select a vehicle";
            return;
        }

        if (string.IsNullOrWhiteSpace(StationName))
        {
            ErrorMessage = "Please enter station name";
            return;
        }

        if (BookingDate == null || BookingTime == null)
        {
            ErrorMessage = "Please select date and time";
            return;
        }

        IsLoading = true;

        try
        {
            var bookingDateTime = BookingDate.Value.Date + BookingTime.Value;
            var bookingTimeStr = bookingDateTime.ToString("yyyy-MM-ddTHH:mm");

            var request = new CreateBookingRequest
            {
                stationName = StationName,
                vehicleId = SelectedVehicle.VehicleId,
                bookingTime = bookingTimeStr
            };

            var response = await _bookingService.CreateBookingAsync(request);

            if (response != null)
            {
                SuccessMessage = $"Booking created successfully! Booking ID: {response.bookingId}";
                
                // Reset form
                SelectedVehicle = null;
                StationName = null;
                BookingDate = DateTime.Today;
                BookingTime = DateTime.Now.TimeOfDay;
            }
            else
            {
                ErrorMessage = "Failed to create booking. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
