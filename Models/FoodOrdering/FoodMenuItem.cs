using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Nattapong_7379_L002.Models.FoodOrdering;

[Table("food_menu_items")]
public sealed class FoodMenuItem
{
    [Column("id")] public int Id { get; set; }
    [Column("category")] public string Category { get; set; } = string.Empty;
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("image_url")] public string ImageUrl { get; set; } = string.Empty;
    [Column("price"), Precision(10, 2)] public decimal Price { get; set; }
    [Column("is_active")] public bool IsActive { get; set; }
}
