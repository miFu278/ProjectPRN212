using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            if (string.IsNullOrWhiteSpace(token)) return 0;

            // Strip possible "Bearer " prefix
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7).Trim();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Try multiple possible claim types that may contain the user id
            var claimTypes = new[] { "id", "userId", ClaimTypes.NameIdentifier, "sub", "nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" };
            string? idClaim = null;
            foreach (var ct in claimTypes)
            {
                idClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, ct, StringComparison.OrdinalIgnoreCase))?.Value;
                if (!string.IsNullOrWhiteSpace(idClaim)) break;
            }

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
