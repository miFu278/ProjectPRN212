# Battery Swap WPF Client

WPF Desktop Application for Battery Swap System

## Features

- ✅ Login with JWT Authentication
- ✅ Role-based routing (Driver/Staff/Admin)
- ✅ Material Design UI
- ✅ MVVM Architecture

## Tech Stack

- .NET 9.0
- WPF
- Material Design In XAML
- CommunityToolkit.Mvvm
- System.IdentityModel.Tokens.Jwt
- QRCoder

## Project Structure

```
BatterySwapWPF/
├── Views/
│   ├── LoginWindow.xaml
│   ├── Driver/
│   │   └── DriverMainWindow.xaml
│   ├── Staff/
│   │   └── StaffMainWindow.xaml
│   └── Admin/
│       └── AdminMainWindow.xaml
├── ViewModels/
│   └── LoginViewModel.cs
├── Services/
│   └── AuthService.cs
├── Models/
│   ├── User.cs
│   └── LoginResponse.cs
├── Helpers/
│   ├── JwtHelper.cs
│   └── SecureStorage.cs
└── Converters/
    ├── InverseBooleanConverter.cs
    └── NullToVisibilityConverter.cs
```

## How to Run

1. Make sure the WebAPI is running on `http://localhost:5000`
2. Run the WPF app:
   ```bash
   cd client/BatterySwapWPF
   dotnet run
   ```

## Test Accounts

Based on your database, you can login with:
- **Driver**: Any user with Role = "Driver"
- **Staff**: Any user with Role = "Staff"  
- **Admin**: Any user with Role = "Admin"

## Next Steps

- [ ] Implement Driver features (Booking, QR Code, History)
- [ ] Implement Staff features (Scan QR, Slot Management)
- [ ] Implement Admin features (User Management, Station Management)
- [ ] Add more pages and navigation
- [ ] Add API services for each feature
