using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class PaymentTransaction
{
    [Key]
    public int ID { get; set; }

    public int User_ID { get; set; }

    public int? Station_ID { get; set; }

    public int? Package_ID { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [StringLength(20)]
    public string? Payment_Method { get; set; }

    [StringLength(100)]
    public string? Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Transaction_Time { get; set; }

    [ForeignKey("Package_ID")]
    [InverseProperty("PaymentTransaction")]
    public virtual Package? Package { get; set; }

    [ForeignKey("Station_ID")]
    [InverseProperty("PaymentTransaction")]
    public virtual Station? Station { get; set; }

    [InverseProperty("Payment")]
    public virtual ICollection<SwapTransaction> SwapTransaction { get; set; } = new List<SwapTransaction>();

    [ForeignKey("User_ID")]
    [InverseProperty("PaymentTransaction")]
    public virtual Users User { get; set; } = null!;
}
