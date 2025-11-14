using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;   // ⬅️ dùng Microsoft.Data.SqlClient (không cần System.Data.SqlClient)
using webAPI.Models;

namespace webAPI.Data
{
    public class BatterySlotRepository
    {
        public async Task<BatterySlot?> FindAndReserveSuitableSlotAsync(
            SqlConnection con,
            int stationId,
            string? batteryModel,
            double minSoH,
            double maxSoH)
        {
            bool hasModelFilter = !string.IsNullOrWhiteSpace(batteryModel);

            var sb = new StringBuilder();
            sb.Append("SELECT TOP 1 ")
              .Append("  s.Slot_ID, s.Slot_Code, s.Slot_Type, s.State, s.Door_State, ")
              .Append("  s.Battery_ID, s.[Condition], s.Last_Update, s.ChargingStation_ID ")
              .Append("FROM dbo.BatterySlot s WITH (UPDLOCK, ROWLOCK) ")
              .Append("JOIN dbo.Battery b ON s.Battery_ID = b.Battery_ID ")
              .Append("JOIN dbo.Battery_Type t ON b.Type_ID = t.ID ")
              .Append("JOIN dbo.Charging_Station cs ON s.ChargingStation_ID = cs.ChargingStation_ID ")
              .Append("WHERE cs.Station_ID = @StationId ")
              .Append("  AND s.State = N'Occupied' ")
              .Append("  AND s.Door_State = N'Closed' ")
              .Append("  AND s.[Condition] = N'Good' ")
              .Append("  AND b.SoH BETWEEN @MinSoH AND @MaxSoH ");

            if (hasModelFilter)
            {
                sb.Append("  AND t.Model = @BatteryModel ");
            }

            sb.Append("ORDER BY b.SoH DESC, s.Last_Update ASC;");

            BatterySlot? picked = null;

            using (var cmd = new SqlCommand(sb.ToString(), con))
            {
                cmd.Parameters.AddWithValue("@StationId", stationId);
                cmd.Parameters.AddWithValue("@MinSoH", minSoH);
                cmd.Parameters.AddWithValue("@MaxSoH", maxSoH);
                if (hasModelFilter)
                {
                    cmd.Parameters.AddWithValue("@BatteryModel", batteryModel!.Trim());
                }

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    picked = new BatterySlot
                    {
                        Slot_ID = reader.GetInt32(reader.GetOrdinal("Slot_ID")),
                        Slot_Code = reader["Slot_Code"] as string,
                        Slot_Type = reader["Slot_Type"] as string,
                        State = reader["State"] as string,
                        Door_State = reader["Door_State"] as string,
                        Battery_ID = reader["Battery_ID"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Battery_ID"]),
                        Condition = reader["Condition"] as string,
                        Last_Update = reader["Last_Update"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["Last_Update"]),
                        ChargingStation_ID = reader.GetInt32(reader.GetOrdinal("ChargingStation_ID"))
                    };
                }
            }

            if (picked == null)
            {
                Console.WriteLine("[DEBUG] No suitable slot found at station " + stationId);
                return null;
            }

            const string updSql =
                "UPDATE dbo.BatterySlot " +
                "SET State = N'Reserved', Last_Update = SYSDATETIME() " +
                "WHERE Slot_ID = @Slot_ID;";

            using (var upCmd = new SqlCommand(updSql, con))
            {
                upCmd.Parameters.AddWithValue("@Slot_ID", picked.Slot_ID);
                int rows = await upCmd.ExecuteNonQueryAsync();

                Console.WriteLine($"[DEBUG] Update rows = {rows} for Slot_ID={picked.Slot_ID}");
                if (rows == 0)
                {
                    Console.WriteLine("[DEBUG] Slot not updated — maybe already reserved?");
                    return null;
                }
            }

            picked.State = "Reserved";
            Console.WriteLine("[INFO] Slot reserved successfully: " + picked.Slot_Code);
            return picked;
        }
    }
}
