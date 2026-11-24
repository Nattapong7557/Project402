using System.Data;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.FoodOrdering;
using Nattapong_7379_L002.ViewModel.FoodOrdering;

namespace Nattapong_7379_L002.Services.FoodOrdering;

public sealed class FoodOrderException(string message) : Exception(message);

public sealed class FoodOrderService(Csi402dbContext db, TimeProvider clock)
{
    public async Task<int> SubmitAsync(int userId, FoodOrderInput input)
    {
        if (!Guid.TryParseExact(input.RequestToken, "N", out var requestToken) || input.TableNumber is not (>= 1 and <= 99))
            throw new FoodOrderException("กรุณาระบุหมายเลขโต๊ะและส่งออเดอร์จากหน้าเมนู");

        var selected = input.Lines.Where(line => line.Quantity > 0).ToList();
        if (input.Lines.Any(line => line.Quantity is < 0 or > 20) || selected.Count == 0 ||
            selected.Select(line => line.MenuItemId).Distinct().Count() != selected.Count)
            throw new FoodOrderException("กรุณาเลือกเมนูและระบุจำนวน 1–20 ต่อรายการ");

        var token = requestToken.ToString("N");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (!await LockAccountAsync(userId))
            throw new FoodOrderException("กรุณาเข้าสู่ระบบใหม่ก่อนส่งออเดอร์");

        var prior = await db.FoodOrders.Where(order => order.UserId == userId && order.RequestToken == token)
            .Select(order => (int?)order.Id).SingleOrDefaultAsync();
        if (prior.HasValue)
        {
            await transaction.CommitAsync();
            return prior.Value;
        }

        var ids = selected.Select(line => line.MenuItemId).ToList();
        var menu = await db.FoodMenuItems.AsNoTracking().Where(item => item.IsActive && ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        if (menu.Count != selected.Count)
            throw new FoodOrderException("บางเมนูไม่พร้อมให้สั่ง กรุณาเลือกใหม่");

        var now = clock.GetUtcNow().UtcDateTime;
        var localTime = FoodPricing.BangkokTime(now);
        var lines = selected.Select(line => new FoodOrderItem
        {
            MenuItemId = line.MenuItemId,
            ItemName = menu[line.MenuItemId].Name,
            UnitPrice = menu[line.MenuItemId].Price,
            Quantity = line.Quantity
        }).ToList();
        var subtotal = lines.Sum(line => line.UnitPrice * line.Quantity);
        var drinkCount = selected.Where(line => FoodPricing.IsDrink(menu[line.MenuItemId].Category)).Sum(line => line.Quantity);
        var discount = FoodPricing.Discount(subtotal, drinkCount, localTime, birthdayEligible: false);
        var order = new FoodOrder
        {
            UserId = userId,
            RequestToken = token,
            TableNumber = input.TableNumber.Value,
            Status = FoodOrderStatus.Submitted,
            Subtotal = subtotal,
            Discount = discount.Amount,
            DiscountCode = discount.Code,
            Total = subtotal - discount.Amount,
            CreatedAt = now,
            Items = lines
        };
        db.FoodOrders.Add(order);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return order.Id;
    }

    public async Task<bool> RequestBillAsync(int orderId, int userId, string? role, string? birthDate)
    {
        if (userId <= 0 || !FoodOrderRules.CanOrder(role)) return false;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // Serialize bill requests for the same member so only one bill can use the annual offer.
        if (!await LockAccountAsync(userId)) return false;
        var transition = FoodOrderRules.Transition(FoodOrderAction.RequestBill);
        var order = await db.FoodOrders.SingleOrDefaultAsync(order => order.Id == orderId &&
            order.UserId == userId && order.Status == transition.From);
        if (order is null) return false;

        if (order.Subtotal >= FoodPricing.BirthdayMinimum && order.DiscountCode != FoodPricing.Birthday)
        {
            var localTime = FoodPricing.BangkokTime(clock.GetUtcNow().UtcDateTime);
            var profile = await db.FoodCustomerProfiles.FindAsync(userId);
            if (profile is null && !string.IsNullOrWhiteSpace(birthDate))
            {
                if (!FoodPricing.TryBirthDate(birthDate, DateOnly.FromDateTime(localTime), out var date))
                    throw new FoodOrderException("กรุณากรอกวันเกิดให้ถูกต้อง หรือล้างช่องนี้หากต้องการเช็คบิลโดยไม่ใช้สิทธิ์วันเกิด");

                profile = new FoodCustomerProfile { UserId = userId, BirthDate = date };
                db.FoodCustomerProfiles.Add(profile);
            }

            var alreadyUsed = profile?.BirthDate.Month == localTime.Month &&
                await db.FoodBirthdayRedemptions.AnyAsync(redemption => redemption.UserId == userId && redemption.Year == localTime.Year);
            if (FoodPricing.BirthdayAtBilling(order, localTime, profile?.BirthDate, alreadyUsed) is { } amount)
            {
                order.DiscountCode = FoodPricing.Birthday;
                order.Discount = amount;
                order.Total = order.Subtotal - amount;
                db.FoodBirthdayRedemptions.Add(new FoodBirthdayRedemption { UserId = userId, Year = localTime.Year, Order = order });
            }
        }

        order.Status = transition.To;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private async Task<bool> LockAccountAsync(int userId) =>
        (await db.Database.SqlQuery<int>($"SELECT id AS `Value` FROM users WHERE id = {userId} FOR UPDATE").ToListAsync()).Count > 0;

    public async Task<bool> AdvanceAsync(int orderId, FoodOrderAction action, int actorId, string? role)
    {
        var transition = FoodOrderRules.Transition(action);
        if (action == FoodOrderAction.RequestBill)
            return await RequestBillAsync(orderId, actorId, role, null);
        if (actorId <= 0 || !FoodOrderRules.IsStaff(role, transition.Role))
            return false;

        var eligible = db.FoodOrders.Where(order => order.Id == orderId && order.Status == transition.From);

        if (action != FoodOrderAction.Pay)
            return await eligible.ExecuteUpdateAsync(update => update.SetProperty(order => order.Status, transition.To)) == 1;

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (await eligible.ExecuteUpdateAsync(update => update.SetProperty(order => order.Status, transition.To)) != 1)
            return false;

        var amount = await db.FoodOrders.Where(order => order.Id == orderId).Select(order => order.Total).SingleAsync();
        db.FoodPayments.Add(new FoodPayment { OrderId = orderId, RecordedByUserId = actorId, Amount = amount, PaidAt = clock.GetUtcNow().UtcDateTime });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
