using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;   // ⬅️ dùng Microsoft.Data.SqlClient (không cần System.Data.SqlClient)
using webAPI.Models;


namespace webAPI.Data
{
    public class DriverPackageRepository
    {
        public async Task<Package?> GetCurrentPackageAsync(SqlConnection con, int userId)
        {
            const string sql =
                "SELECT TOP 1 p.Package_ID, p.Name, p.Description, p.Price, " +
                "       p.Required_SoH, p.MinSoH, p.MaxSoH " +
                "FROM dbo.DriverPackage dp " +
                "JOIN dbo.[Package] p ON p.Package_ID = dp.Package_ID " +
                "WHERE dp.User_ID = @UserId " +
                "  AND (dp.End_date IS NULL OR dp.End_date >= CAST(GETDATE() AS DATE)) " +
                "ORDER BY " +
                "  CASE WHEN dp.End_date IS NULL THEN 1 ELSE 0 END DESC, " +
                "  dp.End_date DESC";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var pkg = new Package
            {
                PackageId = reader.GetInt32(reader.GetOrdinal("Package_ID")),
                Name = reader["Name"] as string,
                Description = reader["Description"] as string,
                Price = (decimal)Convert.ToDouble(reader["Price"]),
                RequiredSoH = Convert.ToDouble(reader["Required_SoH"]),
                MinSoH = Convert.ToInt32(reader["MinSoH"]),
                MaxSoH = Convert.ToInt32(reader["MaxSoH"])
            };

            return pkg;
        }
    }
}
