using System;

namespace Nattapong_7379_L002.Models;

public partial class ServiceItem
{
    public int Id { get; set; }

    /// <summary>Promotion, Makeup, Photographer, Costume</summary>
    public string ServiceType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? BadgeText { get; set; }

    public string? BadgeColor { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
