using CommunityToolkit.Mvvm.ComponentModel;
using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System.Collections.ObjectModel;

namespace BatterySwapWPF.ViewModels.Driver;

public partial class SwapHistoryViewModel : ObservableObject
{
    private readonly HistoryService _historyService;

    [ObservableProperty]
    private ObservableCollection<SwapHistory> swapHistory = new();

    [ObservableProperty]
    private bool isLoading;

    public SwapHistoryViewModel()
    {
        _historyService = new HistoryService();
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _historyService.GetSwapHistoryAsync();
            if (result != null)
            {
                SwapHistory.Clear();
                foreach (var item in result)
                    SwapHistory.Add(item);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
