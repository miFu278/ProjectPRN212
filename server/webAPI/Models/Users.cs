using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

[Index("Email", Name = "UQ__Users__A9D1053405CDDC2E", IsUnique = true)]
public partial class Users
{
    [Key]
    public int ID { get; set; }

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Password { get; set; }

    [StringLength(20)]
    public string? Role { get; set; }

    [StringLength(10)]
    public string? Status { get; set; }

    public int? Station_ID { get; set; }

    [StringLength(500)]
    public string? Avatar_URL { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Booking> Booking { get; set; } = new List<Booking>();

    [InverseProperty("User")]
    public virtual ICollection<Comment> Comment { get; set; } = new List<Comment>();

    [InverseProperty("User")]
    public virtual ICollection<DriverPackage> DriverPackage { get; set; } = new List<DriverPackage>();

    [InverseProperty("User")]
    public virtual ICollection<Password_Reset> Password_Reset { get; set; } = new List<Password_Reset>();

    [InverseProperty("User")]
    public virtual ICollection<PaymentTransaction> PaymentTransaction { get; set; } = new List<PaymentTransaction>();

    [ForeignKey("Station_ID")]
    [InverseProperty("Users")]
    public virtual Station? Station { get; set; }

    [InverseProperty("Driver")]
    public virtual ICollection<SwapTransaction> SwapTransactionDriver { get; set; } = new List<SwapTransaction>();

    [InverseProperty("Staff")]
    public virtual ICollection<SwapTransaction> SwapTransactionStaff { get; set; } = new List<SwapTransaction>();

    [InverseProperty("User")]
    public virtual ICollection<Vehicle> Vehicle { get; set; } = new List<Vehicle>();
}
