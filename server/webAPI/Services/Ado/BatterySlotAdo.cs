// Services/Ado/BatterySlotAdo.cs
using Microsoft.Data.SqlClient;
using System.Data;

namespace webAPI.Services.Ado
{
    public class BatterySlotAdo
    {
        public sealed record PickedSlot(int ChargingStationId, int SlotId);

        /// <summary>
        /// SELECT TOP 1 ... WITH (UPDLOCK, ROWLOCK) + UPDATE Reserved (y như Java)
        /// </summary>
        public async Task<PickedSlot?> FindAndReserveSuitableSlotAsync(
            SqlConnection con, SqlTransaction tx,
            int stationId, string batteryModel, double minSoH, double maxSoH,
            CancellationToken ct)
        {
            var hasModel = !string.IsNullOrWhiteSpace(batteryModel);

            var sql = @"
SELECT TOP 1
  s.Slot_ID, s.Slot_Code, s.Slot_Type, s.State, s.Door_State,
  s.Battery_ID, s.[Condition], s.Last_Update, s.ChargingStation_ID
FROM dbo.BatterySlot s WITH (UPDLOCK, ROWLOCK)
JOIN dbo.Battery b ON s.Battery_ID = b.Battery_ID
JOIN dbo.Battery_Type t ON b.Type_ID = t.ID
JOIN dbo.Charging_Station cs ON s.ChargingStation_ID = cs.ChargingStation_ID
WHERE cs.Station_ID = @station
  AND s.State = N'Occupied'
  AND s.Door_State = N'Closed'
  AND s.[Condition] = N'Good'
  AND b.SoH BETWEEN @min AND @max"
+ (hasModel ? "\n  AND t.Model = @model" : "")
+ @"
ORDER BY b.SoH DESC, s.Last_Update ASC";

            int? slotId = null, csId = null;

            using (var cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddWithValue("@station", stationId);
                cmd.Parameters.AddWithValue("@min", minSoH);
                cmd.Parameters.AddWithValue("@max", maxSoH);
                if (hasModel)
                    cmd.Parameters.AddWithValue("@model", batteryModel.Trim());

                using var rd = await cmd.ExecuteReaderAsync(ct);
                if (await rd.ReadAsync(ct))
                {
                    slotId = Convert.ToInt32(rd["Slot_ID"]);
                    csId = Convert.ToInt32(rd["ChargingStation_ID"]);
                }
            }

            if (slotId == null || csId == null) return null;

            const string upd = @"
UPDATE dbo.BatterySlot
SET State = N'Reserved', Last_Update = SYSDATETIME()
WHERE Slot_ID = @slotId";
            using (var up = new SqlCommand(upd, con, tx))
            {
                up.Parameters.AddWithValue("@slotId", slotId.Value);
                var rows = await up.ExecuteNonQueryAsync(ct);
                if (rows == 0) return null; // race condition
            }

            return new PickedSlot(csId.Value, slotId.Value);
        }
    }
}
