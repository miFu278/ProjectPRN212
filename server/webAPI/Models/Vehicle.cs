using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace webAPI.Models
{
    public partial class Vehicle
    {
        public int Vehicle_ID { get; set; }
        public int User_ID { get; set; }
        public int Model_ID { get; set; }
        public string? Vin { get; set; }
        public string? License_Plate { get; set; }

        // ===== Navigation properties (EF cần) =====
        public virtual Vehicle_Model? Model { get; set; }
        public virtual Users? User { get; set; }
        public virtual ICollection<Booking> Booking { get; set; } = new List<Booking>();

        // ===== Thuộc tính mở rộng (KHÔNG map DB) =====
        [NotMapped]
        public string? Model_Name { get; set; }

        [NotMapped]
        public string? Brand { get; set; }

        [NotMapped]
        public string? Battery_Type { get; set; }
    }
}
