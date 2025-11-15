using BatterySwapWPF.Models;
using BatterySwapWPF.Services;
using BatterySwapWPF.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BatterySwapWPF.ViewModels.Driver;

public partial class BuyPackagesViewModel : ObservableObject
{
    private readonly PackageService _pkgSvc = new();

    public ObservableCollection<Package> Packages { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    public BuyPackagesViewModel()
    {
        LoadPackagesCommand = new AsyncRelayCommand(LoadPackagesAsync);
        BuyCommand = new RelayCommand<Package?>(OnBuyClicked);
        // Auto-load packages when VM is created
        _ = LoadPackagesCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadPackagesCommand { get; }
    public IRelayCommand<Package?> BuyCommand { get; }

    private async Task LoadPackagesAsync()
    {
        IsLoading = true;
        Packages.Clear();
        var list = await _pkgSvc.GetPublicPackagesAsync();
        if (list != null)
        {
            foreach (var p in list) Packages.Add(p);
        }
        IsLoading = false;
    }

    private void OnBuyClicked(Package? pkg)
    {
        if (pkg == null) return;

        var token = SecureStorage.GetToken();
        var userId = 0;
        if (!string.IsNullOrEmpty(token)) userId = Helpers.JwtHelper.GetUserId(token);

        // Build payment URL to server which will redirect to VNPay
        var baseUrl = "http://localhost:5187";
        var url = $"{baseUrl}/api/payment?userId={userId}&packageId={pkg.PackageId}&orderType=buyPackage";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // swallow errors for now
        }
    }
}
