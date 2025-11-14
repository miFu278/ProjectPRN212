using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class Charging_Station
{
    [Key]
    public int ChargingStation_ID { get; set; }

    public int Station_ID { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }

    public int? Slot_Capacity { get; set; }

    [StringLength(20)]
    public string? Slot_Type { get; set; }

    [StringLength(20)]
    public string? Power_Rating { get; set; }

    [InverseProperty("ChargingStation")]
    public virtual ICollection<BatterySlot> BatterySlot { get; set; } = new List<BatterySlot>();

    [InverseProperty("ChargingStation")]
    public virtual ICollection<Booking> Booking { get; set; } = new List<Booking>();

    [ForeignKey("Station_ID")]
    [InverseProperty("Charging_Station")]
    public virtual Station Station { get; set; } = null!;

    [InverseProperty("ChargingStation")]
    public virtual ICollection<SwapTransaction> SwapTransaction { get; set; } = new List<SwapTransaction>();
}
