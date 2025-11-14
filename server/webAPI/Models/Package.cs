using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace webAPI.Models
{
    public partial class Package
    {
        // ==== Cột map trực tiếp với DB (theo scaffold) ====
        public int Package_ID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        // nên dùng decimal cho tiền / SoH vì DB thường là decimal/money
        public decimal Price { get; set; }
        public decimal Required_SoH { get; set; }
        public int MinSoH { get; set; }
        public int MaxSoH { get; set; }
        public string? Status { get; set; }   // "active" | "inactive"

        // ==== Navigation cho EF ====
        public virtual ICollection<Booking> Booking { get; set; } = new List<Booking>();
        public virtual ICollection<DriverPackage> DriverPackage { get; set; } = new List<DriverPackage>();
        public virtual ICollection<PaymentTransaction> PaymentTransaction { get; set; } = new List<PaymentTransaction>();

        // ==== Alias tiện lợi cho code ADO / service cũ ====

        /// <summary>
        /// Alias cho Package_ID để không phải sửa code cũ dùng PackageId.
        /// Không map riêng ra cột nào (chỉ là wrapper).
        /// </summary>
        [NotMapped]
        public int PackageId
        {
            get => Package_ID;
            set => Package_ID = value;
        }

        /// <summary>
        /// Alias double cho Price (decimal) nếu code cũ quen dùng double.
        /// </summary>
        [NotMapped]
        public double PriceDouble
        {
            get => (double)Price;
            set => Price = (decimal)value;
        }

        /// <summary>
        /// Alias double cho Required_SoH (decimal) nếu code cũ dùng RequiredSoH.
        /// </summary>
        [NotMapped]
        public double RequiredSoH
        {
            get => (double)Required_SoH;
            set => Required_SoH = (decimal)value;
        }
    }
}
