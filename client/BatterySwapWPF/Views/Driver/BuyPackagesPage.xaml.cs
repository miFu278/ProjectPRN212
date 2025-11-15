using BatterySwapWPF.Services;
using BatterySwapWPF.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BatterySwapWPF.Views.Driver
{
    public partial class BuyPackagesPage : UserControl, INotifyPropertyChanged
    {
        private readonly PackageService _pkgSvc = new();

        public ObservableCollection<Package> Packages { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public BuyPackagesPage()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += BuyPackagesPage_Loaded;
        }

        private async void BuyPackagesPage_Loaded(object? sender, RoutedEventArgs e)
        {
            await LoadPackagesAsync();
        }

        public async Task LoadPackagesAsync()
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            Packages.Clear();
            try
            {
                var result = await _pkgSvc.GetPublicPackagesAsync();
                if (result != null)
                {
                    foreach (var p in result)
                        Packages.Add(p);

                    if (Packages.Count == 0)
                        StatusMessage = "Không có gói nào để hiển thị.";
                }
                else
                {
                    StatusMessage = "Không tải được gói. Kiểm tra server hoặc kết nối.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi khi tải gói: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Attach this handler to the Buy button's Click in XAML (or use Command binding to call BuyPackageAsync)
        private async void OnBuyClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not Package pkg) return;

            await BuyPackageAsync(pkg);
        }

        private async Task BuyPackageAsync(Package pkg)
        {
            IsLoading = true;
            try
            {
                var token = Helpers.SecureStorage.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    StatusMessage = "Bạn cần đăng nhập để mua gói. Vui lòng đăng nhập.";
                    var res = MessageBox.Show("Bạn cần đăng nhập để mua gói. Mở màn hình đăng nhập?", "Yêu cầu đăng nhập", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        // Open LoginWindow
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var login = new Views.LoginWindow();
                            login.Show();
                        });
                    }

                    return;
                }

                var userId = Helpers.JwtHelper.GetUserId(token);
                if (userId <= 0)
                {
                    StatusMessage = "Không xác định được UserId từ token. Vui lòng đăng nhập lại.";
                    return;
                }

                var url = await _pkgSvc.GetPaymentUrlAsync(userId, pkg.PackageId);
                if (!string.IsNullOrEmpty(url))
                {
                    StatusMessage = "Mở trang thanh toán...";
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                else
                {
                    // Fallback: some server builds return VNPay HTML directly (200 OK) instead of redirecting.
                    // In that case open the server payment endpoint directly so the browser receives the HTML/redirect.
                    var fallback = $"http://localhost:5187/api/payment?userId={userId}&packageId={pkg.PackageId}&orderType=buyPackage";
                    StatusMessage = "Mở trang thanh toán (fallback)...";
                    Process.Start(new ProcessStartInfo { FileName = fallback, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi khi khởi tạo thanh toán: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
