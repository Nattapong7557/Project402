using System.Globalization;
using System.Text.RegularExpressions;

namespace Nattapong_7379_L002.Services;

public sealed record SeatingZone(string Key, string Label, string Name, string BookingName, string Description, string Capacity);
public sealed record SeatingSlot(string Value, string Label, TimeOnly Start, TimeOnly End);

public static partial class TableReservationRules
{
    public static IReadOnlyList<SeatingZone> Zones { get; } =
    [
        new("A", "Zone A", "โซฟาริมหน้าต่าง", "Zone A: โซฟายาวริมหน้าต่างแสงธรรมชาติ (Window Green Sofa)", "โซฟาสีเขียวติดกระจก รับแสงธรรมชาติ", "2–4 ท่าน"),
        new("B", "Zone B", "โต๊ะกลางร้าน", "Zone B: โต๊ะกลางร้าน & โต๊ะไม้ยาวสำหรับกลุ่ม (Central Tables)", "โต๊ะเดี่ยวและโต๊ะไม้ยาวสำหรับกลุ่ม", "2–10 ท่าน"),
        new("C", "Zone C", "โซฟามุมส่วนตัว", "Zone C: โซฟาตัว U พรีเมียมมุมส่วนตัว (VIP Lounge U-Sofa)", "โซฟาทรงตัว U พร้อมฉากไม้ระแนง", "4–8 ท่าน"),
        new("D", "Zone D", "โต๊ะกลมหน้าคาเฟ่", "Zone D: โต๊ะกลมริมกระจกหน้าคาเฟ่ (Front Glass Terrace)", "โต๊ะเล็กริมกระจกด้านหน้า", "1–2 ท่าน"),
        new("bar", "Bar Zone", "ที่นั่งหน้าเคาน์เตอร์", "Bar Zone: โซนหน้าเคาน์เตอร์บาร์กาแฟ (Coffee Bar Zone)", "ชมการชงกาแฟใกล้เคาน์เตอร์", "1–3 ท่าน")
    ];

    public static IReadOnlyList<SeatingSlot> Slots { get; } =
    [
        Slot(8, 30, 10, 30, "รอบเช้า"),
        Slot(11, 0, 13, 0, "รอบเที่ยง"),
        Slot(13, 0, 15, 0, "รอบบ่าย"),
        Slot(15, 0, 17, 0, "รอบบ่ายแก่"),
        Slot(17, 0, 18, 30, "รอบเย็น")
    ];

    private static SeatingSlot Slot(int hour, int minute, int endHour, int endMinute, string label) =>
        new(FormattableString.Invariant($"{hour:00}:{minute:00} - {endHour:00}:{endMinute:00} น."),
            label, new TimeOnly(hour, minute), new TimeOnly(endHour, endMinute));

    public static SeatingZone? FindZone(string? value)
    {
        var zone = value?.Split(':', 2)[0].Trim();
        return Zones.FirstOrDefault(item => string.Equals(zone, item.Key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(zone, item.Label, StringComparison.OrdinalIgnoreCase));
    }

    public static SeatingSlot? FindSlot(string? value) =>
        Slots.FirstOrDefault(slot => string.Equals(slot.Value, value?.Trim(), StringComparison.Ordinal));

    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string LegacyDateAlias(DateOnly date) =>
        FormattableString.Invariant($"{date.Year + 543:0000}-{date.Month:00}-{date.Day:00}");

    public static bool TryDate(string? value, out DateOnly date)
    {
        // Older Thai-localized forms sometimes send a Buddhist year with ISO month and day.
        if (value is { Length: 10 } && int.TryParse(value.AsSpan(0, 4), NumberStyles.None,
            CultureInfo.InvariantCulture, out var year) && year >= 2500)
            value = (year - 543).ToString("D4", CultureInfo.InvariantCulture) + value[4..];

        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static bool HasStarted(DateOnly date, TimeOnly start, DateTime localNow) =>
        date.ToDateTime(start) <= localNow;

    [GeneratedRegex(@"^\s*(\d{1,2}:\d{2})\s*[-–—]\s*(\d{1,2}:\d{2})(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex TimeRangePattern();

    public static bool TryTimeRange(string? value, out TimeOnly start, out TimeOnly end)
    {
        start = end = default;
        var match = TimeRangePattern().Match(value ?? string.Empty);
        return match.Success &&
            TimeOnly.TryParseExact(match.Groups[1].Value, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out start) &&
            TimeOnly.TryParseExact(match.Groups[2].Value, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out end) &&
            start < end;
    }

    public static bool Overlaps(string? existingSlot, TimeOnly start, TimeOnly end) =>
        // An active legacy reservation with an unknown time cannot be declared available.
        !TryTimeRange(existingSlot, out var existingStart, out var existingEnd) ||
        (start < existingEnd && existingStart < end);
}
