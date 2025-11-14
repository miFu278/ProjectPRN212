using CommunityToolkit.Mvvm.ComponentModel;
using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System.Collections.ObjectModel;

namespace BatterySwapWPF.ViewModels.Driver;

public partial class PackageHistoryViewModel : ObservableObject
{
    private readonly HistoryService _historyService;

    [ObservableProperty]
    private ObservableCollection<PackageHistory> packageHistory = new();

    [ObservableProperty]
    private bool isLoading;

    public PackageHistoryViewModel()
    {
        _historyService = new HistoryService();
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _historyService.GetPackageHistoryAsync();
            if (result != null)
            {
                PackageHistory.Clear();
                foreach (var item in result)
                    PackageHistory.Add(item);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
