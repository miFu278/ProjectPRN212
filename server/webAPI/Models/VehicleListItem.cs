// Models/VehicleListItem.cs
namespace webAPI.Models
{
    public class VehicleListItem
    {
        public int Vehicle_ID { get; set; }
        public int User_ID { get; set; }
        public int Model_ID { get; set; }
        public string? Vin { get; set; }
        public string? License_Plate { get; set; }

        // Thuộc tính từ bảng Vehicle_Model (join)
        public string? Model_Name { get; set; }
        public string? Brand { get; set; }
        public string? Battery_Type { get; set; }
    }
}
