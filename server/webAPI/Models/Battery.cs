using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class Battery
{
    [Key]
    public int Battery_ID { get; set; }

    [StringLength(50)]
    public string? Serial_Number { get; set; }

    public double? Resistance { get; set; }

    public double? SoH { get; set; }

    public int? Type_ID { get; set; }

    [InverseProperty("Battery")]
    public virtual ICollection<BatterySlot> BatterySlot { get; set; } = new List<BatterySlot>();

    [ForeignKey("Type_ID")]
    [InverseProperty("Battery")]
    public virtual Battery_Type? Type { get; set; }
}
