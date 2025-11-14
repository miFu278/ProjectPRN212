using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System.Collections.ObjectModel;

namespace BatterySwapWPF.ViewModels.Driver;

public partial class MyVehiclesViewModel : ObservableObject
{
    private readonly VehicleService _vehicleService;

    [ObservableProperty]
    private ObservableCollection<Vehicle> vehicles = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public MyVehiclesViewModel()
    {
        _vehicleService = new VehicleService();
        _ = LoadVehiclesAsync();
    }

    [RelayCommand]
    private async Task LoadVehiclesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _vehicleService.GetMyVehiclesAsync();

            if (result != null)
            {
                Vehicles.Clear();
                foreach (var vehicle in result)
                {
                    Vehicles.Add(vehicle);
                }
            }
            else
            {
                ErrorMessage = "Failed to load vehicles";
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
