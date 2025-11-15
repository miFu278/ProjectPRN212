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
    private readonly StationService _stationService;

    [ObservableProperty]
    private ObservableCollection<Vehicle> vehicles = new();

    [ObservableProperty]
    private Vehicle? selectedVehicle;

    [ObservableProperty]
    private ObservableCollection<Station> stations = new();

    [ObservableProperty]
    private Station? selectedStation;

    [ObservableProperty]
    private DateTime? bookingDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? bookingTime = DateTime.Now;

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
        _stationService = new StationService();
        _ = LoadVehiclesAsync();
        _ = LoadStationsAsync();
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

    private async Task LoadStationsAsync()
    {
        try
        {
            var result = await _stationService.GetStationsAsync();
            if (result != null)
            {
                Stations.Clear();
                foreach (var st in result)
                    Stations.Add(st);
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

        if (SelectedStation == null)
        {
            ErrorMessage = "Please select a station";
            return;
        }

        if (BookingDate == null || BookingTime == null)
        {
            ErrorMessage = "Please select date and time";
            return;
        }

        // Combine date and time and validate: cannot book in the past, and booking must be for today only.
        var combinedBooking = BookingDate.Value.Date + BookingTime.Value.TimeOfDay;
        var now = DateTime.Now;

        if (combinedBooking < now)
        {
            ErrorMessage = "Cannot create booking in the past";
            return;
        }

        if (combinedBooking.Date != now.Date)
        {
            ErrorMessage = "Bookings are only allowed for today";
            return;
        }

        IsLoading = true;

        try
        {
            // BookingTime is a DateTime (time part used). Combine date and time parts
            var bookingDateTime = BookingDate.Value.Date + BookingTime.Value.TimeOfDay;
            var bookingTimeStr = bookingDateTime.ToString("yyyy-MM-ddTHH:mm");

            var request = new CreateBookingRequest
            {
                stationName = SelectedStation?.Name,
                vehicleId = SelectedVehicle.VehicleId,
                bookingTime = bookingTimeStr
            };

            var response = await _bookingService.CreateBookingAsync(request);

            if (response != null)
            {
                SuccessMessage = $"Booking created successfully! Booking ID: {response.bookingId}";
                
                // Reset form
                SelectedVehicle = null;
                SelectedStation = null;
                BookingDate = DateTime.Today;
                BookingTime = DateTime.Now;
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
