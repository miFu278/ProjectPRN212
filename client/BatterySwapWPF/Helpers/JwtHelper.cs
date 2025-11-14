using System.IdentityModel.Tokens.Jwt;

namespace BatterySwapWPF.Helpers;

public static class JwtHelper
{
    public static string GetRole(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    public static int GetUserId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == "sub")?.Value;
            return int.TryParse(idClaim, out var id) ? id : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
