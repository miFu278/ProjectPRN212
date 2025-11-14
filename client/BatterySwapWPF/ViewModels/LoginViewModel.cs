using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BatterySwapWPF.Services;
using BatterySwapWPF.Helpers;
using System.Windows;

namespace BatterySwapWPF.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? password;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoading;

    public LoginViewModel()
    {
        _authService = new AuthService();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter email and password";
            return;
        }

        IsLoading = true;

        try
        {
            var response = await _authService.LoginAsync(Email, Password);

            if (response?.Token != null)
            {
                SecureStorage.SaveToken(response.Token);
                var role = JwtHelper.GetRole(response.Token);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Window? targetWindow = role.ToLower() switch
                    {
                        "driver" => new Views.Driver.DriverMainWindow(),
                        "staff" => new Views.Staff.StaffMainWindow(),
                        "admin" => new Views.Admin.AdminMainWindow(),
                        _ => null
                    };

                    if (targetWindow != null)
                    {
                        targetWindow.Show();
                        Application.Current.Windows.OfType<Views.LoginWindow>().FirstOrDefault()?.Close();
                    }
                    else
                    {
                        ErrorMessage = $"Unknown role: {role}";
                    }
                });
            }
            else
            {
                ErrorMessage = "Invalid email or password";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
