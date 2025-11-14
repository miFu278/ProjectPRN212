using System;
using System.Collections.Generic;

namespace webAPI.Models
{
    public partial class Booking
    {
        public int Booking_ID { get; set; }
        public int User_ID { get; set; }
        public int Vehicle_ID { get; set; }
        public int Package_ID { get; set; }

        public int Station_ID { get; set; }
        public int ChargingStation_ID { get; set; }
        public int Slot_ID { get; set; }

        public string? Battery_Request { get; set; }     // model pin (nếu có)
        public string? Status { get; set; }              // Reserved | Completed | Cancelled...
        public DateTime Booking_Time { get; set; }
        public DateTime Expired_Date { get; set; }
        public string? Qr_Code { get; set; }

        // ==== Navigation cho EF (theo BatterySwapContext) ====
        public virtual Charging_Station? ChargingStation { get; set; }
        public virtual Package? Package { get; set; }
        public virtual BatterySlot? Slot { get; set; }
        public virtual Station? Station { get; set; }
        public virtual Users? User { get; set; }
        public virtual Vehicle? Vehicle { get; set; }

        public virtual ICollection<SwapTransaction> SwapTransaction { get; set; } = new List<SwapTransaction>();
    }
}
