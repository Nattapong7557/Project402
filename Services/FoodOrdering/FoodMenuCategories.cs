namespace Nattapong_7379_L002.Services.FoodOrdering;

public sealed record FoodMenuCategory(string Key, string Label, string Icon);

public static class FoodMenuCategories
{
    public static IReadOnlyList<FoodMenuCategory> All { get; } =
    [
        new("all", "ทั้งหมด", "M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z"),
        new("coffee", "กาแฟ", "M4 9h12v5a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5V9Zm12 1h2a3 3 0 0 1 0 6h-2M3 22h16M8 6V3m5 3V3"),
        new("tea", "ชา", "M4 11h16l-2 6a4 4 0 0 1-4 3h-4a4 4 0 0 1-4-3l-2-6Zm6-3c0-3 2-5 7-6 0 4-2 7-6 7"),
        new("beverage", "เครื่องดื่มเย็น", "M5 8h14l-2 13H7L5 8Zm8 0 1-5 4-1M6 12h12m-8 4h1m3 2h1"),
        new("bread", "ขนมปังและโทสต์", "M3 16c0-4 3-8 7-10l2 3 2-3c4 2 7 6 7 10l-3 2-4-3h-4l-4 3-3-2Zm7-10-2 10m6-10 2 10"),
        new("cake", "เค้ก", "M4 12h16v9H4zM4 15c2 0 2 2 4 2s2-2 4-2 2 2 4 2 2-2 4-2M12 12V8m0-2c-2-1-2-3 0-4 2 1 2 3 0 4Z"),
        new("donut", "โดนัท", "M3 12a9 9 0 1 0 18 0 9 9 0 0 0-18 0Zm6.5 0a2.5 2.5 0 1 0 5 0 2.5 2.5 0 0 0-5 0ZM7 8h1m8-1 1 1m-1 8h1m-9 1v1"),
        new("western", "อาหารตะวันตก", "M3 17h18M5 17a7 7 0 1 1 14 0m-7-7V7m-2 0h4M2 21h20")
    ];
}
