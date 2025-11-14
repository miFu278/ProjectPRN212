using System.Windows;
using BatterySwapWPF.Helpers;

namespace BatterySwapWPF.Views.Driver;

public partial class DriverMainWindow : Window
{
    public DriverMainWindow()
    {
        InitializeComponent();
        
        // Navigate to My Vehicles by default
        ContentFrame.Navigate(new MyVehiclesPage());
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        SecureStorage.ClearToken();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }

    private void BtnMyVehicles_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new MyVehiclesPage());
    }

    private void BtnMyBookings_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new MyBookingsPage());
    }

    private void BtnSwapHistory_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new SwapHistoryPage());
    }

    private void BtnPackageHistory_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new PackageHistoryPage());
    }
}
