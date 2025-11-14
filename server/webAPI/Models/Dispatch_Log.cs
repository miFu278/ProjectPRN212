using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

[Index("BatteryType_Request_ID", Name = "IX_Dispatch_BatteryType")]
[Index("Request_Time", Name = "IX_Dispatch_RequestTime")]
[Index("Station_Respond_ID", Name = "IX_Dispatch_Respond_ID")]
[Index("Station_Request_ID", Name = "IX_Dispatch_Station_Request")]
public partial class Dispatch_Log
{
    [Key]
    public int ID { get; set; }

    public int Station_Request_ID { get; set; }

    public int? Station_Respond_ID { get; set; }

    public int BatteryType_Request_ID { get; set; }

    public int Quantity_Type_Good { get; set; }

    public int Quantity_Type_Average { get; set; }

    public int Quantity_Type_Bad { get; set; }

    [Precision(0)]
    public DateTime Request_Time { get; set; }

    [Precision(0)]
    public DateTime? Respond_Time { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [ForeignKey("BatteryType_Request_ID")]
    [InverseProperty("Dispatch_Log")]
    public virtual Battery_Type BatteryType_Request { get; set; } = null!;

    [ForeignKey("Station_Request_ID")]
    [InverseProperty("Dispatch_Log")]
    public virtual Station Station_Request { get; set; } = null!;
}
