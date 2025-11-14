using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class Password_Reset
{
    [Key]
    public int ID { get; set; }

    public int? User_ID { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string OTP { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? Created_At { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Expired_At { get; set; }

    public bool? Is_Used { get; set; }

    [ForeignKey("User_ID")]
    [InverseProperty("Password_Reset")]
    public virtual Users? User { get; set; }
}
