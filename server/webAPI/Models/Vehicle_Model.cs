using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class Vehicle_Model
{
    [Key]
    public int Model_ID { get; set; }

    [StringLength(80)]
    public string Model_Name { get; set; } = null!;

    [StringLength(60)]
    public string? Brand { get; set; }

    [StringLength(50)]
    public string? Battery_Type { get; set; }

    [InverseProperty("Model")]
    public virtual ICollection<Vehicle> Vehicle { get; set; } = new List<Vehicle>();
}
