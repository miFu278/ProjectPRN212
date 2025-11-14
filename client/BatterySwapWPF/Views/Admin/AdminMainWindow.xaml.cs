using System.Windows;
using BatterySwapWPF.Helpers;

namespace BatterySwapWPF.Views.Admin;

public partial class AdminMainWindow : Window
{
    public AdminMainWindow()
    {
        InitializeComponent();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        SecureStorage.ClearToken();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }
}
