using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace BatterySwapWPF.ViewModels.Driver;

public partial class MyBookingsViewModel : ObservableObject
{
    private readonly BookingService _bookingService;

    [ObservableProperty]
    private ObservableCollection<Booking> bookings = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasNoBookings;

    public MyBookingsViewModel()
    {
        _bookingService = new BookingService();
        _ = LoadBookingsAsync();
    }

    [RelayCommand]
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
                {
                    Bookings.Add(booking);
                }
                HasNoBookings = Bookings.Count == 0;
            }
        }
        catch
        {
            HasNoBookings = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewQr(Booking booking)
    {
        if (string.IsNullOrEmpty(booking.QrCode))
        {
            MessageBox.Show("QR Code not available", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var qrWindow = new Views.Driver.QrCodeWindow(booking);
        qrWindow.ShowDialog();
    }
}
