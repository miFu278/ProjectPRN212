using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

[Index("User_ID", "Swap_ID", Name = "UX_Comment_User_Swap", IsUnique = true)]
public partial class Comment
{
    [Key]
    public int Comment_ID { get; set; }

    public int User_ID { get; set; }

    public string? Content { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Time_Post { get; set; }

    public int? Swap_ID { get; set; }

    [ForeignKey("Swap_ID")]
    [InverseProperty("Comment")]
    public virtual SwapTransaction? Swap { get; set; }

    [ForeignKey("User_ID")]
    [InverseProperty("Comment")]
    public virtual Users User { get; set; } = null!;
}
