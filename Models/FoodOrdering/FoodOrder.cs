using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Nattapong_7379_L002.Models.FoodOrdering;

public enum FoodOrderStatus { Submitted, InKitchen, Ready, Served, AwaitingPayment, Paid }

[Table("food_orders")]
public sealed class FoodOrder
{
    [Column("id")] public int Id { get; set; }
    [Column("user_id")] public int? UserId { get; set; }
    [Column("request_token")] public string RequestToken { get; set; } = string.Empty;
    [Column("table_number")] public int TableNumber { get; set; }
    [Column("status")] public FoodOrderStatus Status { get; set; }
    [Column("subtotal"), Precision(10, 2)] public decimal Subtotal { get; set; }
    [Column("discount"), Precision(10, 2)] public decimal Discount { get; set; }
    [Column("discount_code")] public string? DiscountCode { get; set; }
    [Column("total"), Precision(10, 2)] public decimal Total { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    public List<FoodOrderItem> Items { get; set; } = [];
    public FoodPayment? Payment { get; set; }
}

[Table("food_order_items")]
public sealed class FoodOrderItem
{
    [Column("id")] public int Id { get; set; }
    [Column("order_id")] public int OrderId { get; set; }
    [Column("menu_item_id")] public int MenuItemId { get; set; }
    [Column("item_name")] public string ItemName { get; set; } = string.Empty;
    [Column("unit_price"), Precision(10, 2)] public decimal UnitPrice { get; set; }
    [Column("quantity")] public int Quantity { get; set; }
}

[Table("food_payments")]
public sealed class FoodPayment
{
    [System.ComponentModel.DataAnnotations.Key, Column("order_id")] public int OrderId { get; set; }
    [Column("recorded_by_user_id")] public int? RecordedByUserId { get; set; }
    [Column("amount"), Precision(10, 2)] public decimal Amount { get; set; }
    [Column("paid_at")] public DateTime PaidAt { get; set; }
}
