using System;
using System.Collections.Generic;

namespace Nattapong_7379_L002.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public string? Package { get; set; } = "Premium";

    public virtual Staff? Staff { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
