using System;
using System.Collections.Generic;

namespace Nattapong_7379_L002.Models;

public partial class Staff
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
