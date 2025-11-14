using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class Battery_Type
{
    [Key]
    public int ID { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [StringLength(50)]
    public string? Specification { get; set; }

    [Column(TypeName = "decimal(5, 3)")]
    public decimal? Nominal_Resistance { get; set; }

    [InverseProperty("Type")]
    public virtual ICollection<Battery> Battery { get; set; } = new List<Battery>();

    [InverseProperty("BatteryType_Request")]
    public virtual ICollection<Dispatch_Log> Dispatch_Log { get; set; } = new List<Dispatch_Log>();
}
