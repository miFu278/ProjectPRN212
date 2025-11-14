// Services/Ado/DriverPackageAdo.cs
using Microsoft.Data.SqlClient;
using System.Data;

namespace webAPI.Services.Ado
{
    public class DriverPackageAdo
    {
        public async Task<(int PackageId, int MinSoH, int MaxSoH)?> GetCurrentPackageAsync(SqlConnection con, SqlTransaction tx, int userId, CancellationToken ct)
        {
            const string sql = @"
SELECT TOP 1 p.Package_ID, p.MinSoH, p.MaxSoH
FROM dbo.DriverPackage dp
JOIN dbo.[Package] p ON p.Package_ID = dp.Package_ID
WHERE dp.User_ID=@uid AND (dp.End_date IS NULL OR dp.End_date >= CAST(GETDATE() AS DATE))
ORDER BY CASE WHEN dp.End_date IS NULL THEN 1 ELSE 0 END DESC,
         dp.End_date DESC";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (!await rd.ReadAsync(ct)) return null;
            var pkgId = (int)rd["Package_ID"];
            var minSoH = Convert.ToInt32(rd["MinSoH"]);
            var maxSoH = Convert.ToInt32(rd["MaxSoH"]);
            return (pkgId, minSoH, maxSoH);
        }
    }
}
