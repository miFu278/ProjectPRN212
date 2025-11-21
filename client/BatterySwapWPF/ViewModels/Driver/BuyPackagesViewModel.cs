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
    [ObservableProperty]
    private string statusMessage = string.Empty;

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
        if (list == null)
        {
            StatusMessage = "Không thể tải danh sách gói (kiểm tra server).";
        }
        else if (list.Count == 0)
        {
            StatusMessage = "Hiện chưa có gói pin nào.";
        }
        else
        {
            StatusMessage = string.Empty;
            foreach (var p in list) Packages.Add(p);
        }
        IsLoading = false;
    }

    private void OnBuyClicked(Package? pkg)
    {
        if (pkg == null) return;
        _ = BuyPackageAsync(pkg);
    }

    private async Task BuyPackageAsync(Package pkg)
    {
        IsLoading = true;
        try
        {
            var token = SecureStorage.GetToken();
            var userId = 0;
            if (!string.IsNullOrEmpty(token)) userId = Helpers.JwtHelper.GetUserId(token);

            var paymentUrl = await _pkgSvc.GetPaymentUrlAsync(userId, pkg.PackageId);
            if (!string.IsNullOrEmpty(paymentUrl))
            {
                StatusMessage = "Opening VNPay checkout...";
                Process.Start(new ProcessStartInfo { FileName = paymentUrl, UseShellExecute = true });
            }
            else
            {
                StatusMessage = "Không thể lấy URL thanh toán từ server. Kiểm tra log server.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Lỗi khi tạo thanh toán: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
