using CommunityToolkit.Mvvm.ComponentModel;
using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System.Collections.ObjectModel;

namespace BatterySwapWPF.ViewModels.Staff;

public partial class AllBookingsViewModel : ObservableObject
{
    private readonly BookingService _bookingService;

    [ObservableProperty]
    private ObservableCollection<Booking> bookings = new();

    [ObservableProperty]
    private bool isLoading;

    public AllBookingsViewModel()
    {
        _bookingService = new BookingService();
        _ = LoadBookingsAsync();
    }

    private async Task LoadBookingsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _bookingService.GetMyBookingsAsync();
            if (result != null)
            {
                Bookings.Clear();
                foreach (var booking in result)
                    Bookings.Add(booking);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
