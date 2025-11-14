using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

[Index("IsActive", Name = "IX_Station_IsActive")]
public partial class Station
{
    [Key]
    public int Station_ID { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string Address { get; set; } = null!;

    public bool IsActive { get; set; }

    [Column(TypeName = "decimal(10, 8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11, 8)")]
    public decimal? Longitude { get; set; }

    [InverseProperty("Station")]
    public virtual ICollection<Booking> Booking { get; set; } = new List<Booking>();

    [InverseProperty("Station")]
    public virtual ICollection<Charging_Station> Charging_Station { get; set; } = new List<Charging_Station>();

    [InverseProperty("Station_Request")]
    public virtual ICollection<Dispatch_Log> Dispatch_Log { get; set; } = new List<Dispatch_Log>();

    [InverseProperty("Station")]
    public virtual ICollection<PaymentTransaction> PaymentTransaction { get; set; } = new List<PaymentTransaction>();

    [InverseProperty("Station")]
    public virtual ICollection<SwapTransaction> SwapTransaction { get; set; } = new List<SwapTransaction>();

    [InverseProperty("Station")]
    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
