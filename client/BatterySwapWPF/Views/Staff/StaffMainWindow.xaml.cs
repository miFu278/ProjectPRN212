using System.Windows;
using BatterySwapWPF.Helpers;

namespace BatterySwapWPF.Views.Staff;

public partial class StaffMainWindow : Window
{
    public StaffMainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigate(new AllBookingsPage());
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        SecureStorage.ClearToken();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }

    private void BtnAllBookings_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new AllBookingsPage());
    }

    private void BtnCheckIn_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new CheckInPage());
    }
}
