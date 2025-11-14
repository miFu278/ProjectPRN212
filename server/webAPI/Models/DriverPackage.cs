using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class DriverPackage
{
    [Key]
    public int ID { get; set; }

    public int User_ID { get; set; }

    public int Package_ID { get; set; }

    public DateOnly? Start_date { get; set; }

    public DateOnly? End_date { get; set; }

    [ForeignKey("Package_ID")]
    [InverseProperty("DriverPackage")]
    public virtual Package Package { get; set; } = null!;

    [ForeignKey("User_ID")]
    [InverseProperty("DriverPackage")]
    public virtual Users User { get; set; } = null!;
}
