using System.Windows;
using System.Windows.Media.Imaging;
using BatterySwapWPF.Models;

namespace BatterySwapWPF.Views.Driver;

public partial class QrCodeWindow : Window
{
    public QrCodeWindow(Booking booking)
    {
        InitializeComponent();

        // Display booking info
        BookingIdText.Text = $"Booking #{booking.BookingId}";
        StationText.Text = booking.StationName ?? "Station";

        // Decode and display QR code
        if (!string.IsNullOrEmpty(booking.QrCode))
        {
            try
            {
                var imageBytes = Convert.FromBase64String(booking.QrCode);
                var bitmap = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }
                QrImage.Source = bitmap;
            }
            catch
            {
                MessageBox.Show("Failed to load QR code", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
