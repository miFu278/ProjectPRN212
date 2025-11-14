using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace webAPI.Models
{
    public partial class BatterySlot
    {
        public int Slot_ID { get; set; }
        public string? Slot_Code { get; set; }
        public string? Slot_Type { get; set; }
        public string? State { get; set; }
        public string? Door_State { get; set; }
        public int? Battery_ID { get; set; }       // có thể null
        public string? Condition { get; set; }
        public DateTime? Last_Update { get; set; }
        public int ChargingStation_ID { get; set; }

        // ==== Navigation cho EF (theo BatterySwapContext) ====
        public virtual Battery? Battery { get; set; }
        public virtual Charging_Station? ChargingStation { get; set; }
        public virtual ICollection<Booking> Booking { get; set; } = new List<Booking>();

        // ==== Mở rộng cho FE (KHÔNG map DB) ====
        [NotMapped]
        public string? ChargingStationName { get; set; }

        [NotMapped]
        public string? ChargingSlotType { get; set; }   // FE dùng để fallback lọc Lithium/LFP

        [NotMapped]
        public string? BatteryModel { get; set; }

        [NotMapped]
        public double BatterySoH { get; set; }

        [NotMapped]
        public string? BatterySerial { get; set; }

        [NotMapped]
        public int? BatteryTypeId { get; set; }         // 1 = Li-ion, 2 = LFP

        [NotMapped]
        public string? BatteryChemistry { get; set; }   // "Lithium-ion" | "LFP" | "Unknown"
    }
}
