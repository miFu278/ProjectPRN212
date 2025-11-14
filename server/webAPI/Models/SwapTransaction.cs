using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

[Index("Booking_ID", Name = "IX_SwapTransaction_BookingID")]
public partial class SwapTransaction
{
    [Key]
    public int ID { get; set; }

    public int Driver_ID { get; set; }

    public int? Staff_ID { get; set; }

    public int Station_ID { get; set; }

    public int? Old_Battery { get; set; }

    public int? New_Battery { get; set; }

    public double? SoH_Old { get; set; }

    public double? SoH_New { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Fee { get; set; }

    public int? Payment_ID { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Swap_Time { get; set; }

    public int? Booking_ID { get; set; }

    public int? ChargingStation_ID { get; set; }

    [ForeignKey("Booking_ID")]
    [InverseProperty("SwapTransaction")]
    public virtual Booking? Booking { get; set; }

    [ForeignKey("ChargingStation_ID")]
    [InverseProperty("SwapTransaction")]
    public virtual Charging_Station? ChargingStation { get; set; }

    [InverseProperty("Swap")]
    public virtual ICollection<Comment> Comment { get; set; } = new List<Comment>();

    [ForeignKey("Driver_ID")]
    [InverseProperty("SwapTransactionDriver")]
    public virtual Users Driver { get; set; } = null!;

    [ForeignKey("Payment_ID")]
    [InverseProperty("SwapTransaction")]
    public virtual PaymentTransaction? Payment { get; set; }

    [ForeignKey("Staff_ID")]
    [InverseProperty("SwapTransactionStaff")]
    public virtual Users? Staff { get; set; }

    [ForeignKey("Station_ID")]
    [InverseProperty("SwapTransaction")]
    public virtual Station Station { get; set; } = null!;
}
