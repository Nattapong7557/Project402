using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services.FoodOrdering;

namespace Nattapong_7379_L002.Services;

public enum ZoneReservationResult { Saved, Occupied, Busy, Started }

public sealed class TableReservations(Csi402dbContext db, TimeProvider clock)
{
    public DateTime LocalNow => FoodPricing.BangkokTime(clock.GetUtcNow().UtcDateTime);

    public async Task<IReadOnlyList<string>> UnavailableAsync(SeatingZone zone, DateOnly date)
    {
        var occupied = await ActiveTimesAsync(zone, date);
        var now = LocalNow;
        return TableReservationRules.Slots
            .Where(slot => TableReservationRules.HasStarted(date, slot.Start, now) ||
                occupied.Any(existing => TableReservationRules.Overlaps(existing, slot.Start, slot.End)))
            .Select(slot => slot.Value).ToArray();
    }

    public async Task<ZoneReservationResult> TrySaveAsync(Booking booking, SeatingZone zone, DateOnly date)
    {
        var connection = db.Database.GetDbConnection();
        var key = $"cafe:{connection.Database}:zone:{zone.Key}:{TableReservationRules.FormatDate(date)}";
        await db.Database.OpenConnectionAsync();
        try
        {
            // MySQL advisory locks serialize overlapping writes across requests and application instances.
            if (Convert.ToInt32(await LockCommandAsync(connection, "SELECT GET_LOCK(@name, 5)", key)) != 1)
                return ZoneReservationResult.Busy;

            try
            {
                var hasRange = TableReservationRules.TryTimeRange(booking.TimeSlot, out var start, out var end);
                if (hasRange && booking.Status == "Pending" && TableReservationRules.HasStarted(date, start, LocalNow))
                    return ZoneReservationResult.Started;

                var occupied = await ActiveTimesAsync(zone, date);
                if (hasRange ? occupied.Any(existing => TableReservationRules.Overlaps(existing, start, end)) : occupied.Count > 0)
                    return ZoneReservationResult.Occupied;

                db.Bookings.Add(booking);
                await db.SaveChangesAsync();
                return ZoneReservationResult.Saved;
            }
            finally
            {
                await LockCommandAsync(connection, "SELECT RELEASE_LOCK(@name)", key);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<List<string>> ActiveTimesAsync(SeatingZone zone, DateOnly date)
    {
        var canonical = TableReservationRules.FormatDate(date);
        // Older Thai-culture pages stored the Buddhist year in the same ISO-shaped string.
        var legacy = TableReservationRules.LegacyDateAlias(date);
        var records = await db.Bookings.AsNoTracking().Where(booking => booking.ServiceType == "Table" &&
            (booking.BookingDate == canonical || booking.BookingDate == legacy) &&
            (booking.Status == "Pending" || booking.Status == "Confirmed" || booking.Status == "QueueConfirmed" ||
             booking.Status == "Checked-In" || booking.Status == "Preparing" || booking.Status == "Ready" ||
             booking.Status == "Served" || booking.Status == "Paid"))
            .Select(booking => new { booking.ItemName, booking.TimeSlot }).ToListAsync();

        return records.Where(booking => TableReservationRules.FindZone(booking.ItemName)?.Key == zone.Key)
            .Select(booking => booking.TimeSlot).ToList();
    }

    private static async Task<object?> LockCommandAsync(DbConnection connection, string sql, string name)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = name;
        command.Parameters.Add(parameter);
        return await command.ExecuteScalarAsync();
    }
}
