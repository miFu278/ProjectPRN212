namespace BatterySwapWPF.Helpers;

public static class SecureStorage
{
    private const string TokenKey = "JwtToken";

    public static void SaveToken(string token)
    {
        Properties.Settings.Default[TokenKey] = token;
        Properties.Settings.Default.Save();
    }

    public static string? GetToken()
    {
        return Properties.Settings.Default[TokenKey]?.ToString();
    }

    public static void ClearToken()
    {
        Properties.Settings.Default[TokenKey] = string.Empty;
        Properties.Settings.Default.Save();
    }
}
