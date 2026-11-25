using System.Globalization;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.FoodOrdering;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.ViewModel;
using Nattapong_7379_L002.ViewModel.FoodOrdering;

using Nattapong_7379_L002.Services.Membership;

var checks = new (string Name, Action Run)[]
{
    ("Customer status presentation covers active, final and legacy states", () =>
    {
        var states = new[] { "Pending", "QueueConfirmed", "Confirmed", "Checked-In", "Preparing", "Ready", "Served", "Paid", "Completed", "Cancelled", "unknown" };

        var descriptions = states.Select(CustomerBookingStatus.Describe).ToArray();

        Equal("20,45,45,45,65,65,85,100,100,0,0", string.Join(",", descriptions.Select(status => status.Progress)));
        Equal("ff-card-workspace-badge-rose", descriptions[9].BadgeClass);
        Equal("ไม่ทราบสถานะ", descriptions[10].Label);
        Equal(false, descriptions[7].Label == descriptions[8].Label);
    }),
    ("The customer cancel rule matches the statuses that display the cancel action", () =>
    {
        var states = new[] { "Pending", "QueueConfirmed", "Confirmed", "Checked-In", "Preparing", "Ready", "Served", "Paid", "Completed", "Cancelled", "unknown" };
        var canCancel = CustomerBookingStatus.Cancellable.Compile(preferInterpretation: true);

        var eligible = states.Where(status => canCancel(new Booking { Status = status }));
        var visible = states.Where(status => CustomerBookingStatus.Describe(status).CanCancel);

        Equal("Pending,QueueConfirmed,Confirmed", string.Join(",", eligible));
        Equal(true, eligible.SequenceEqual(visible));
    }),
    ("Monthly profit adds the three income sources and subtracts expenses", () =>
    {
        var month = new DateOnly(2026, 11, 1);
        var records = new[]
        {
            new Booking { ServiceType = "Income", ItemName = "Rental (฿100.00) (฿1,234.50)" },
            new Booking { ServiceType = "Income", ItemName = "Other (฿25.50)" },
            new Booking { ServiceType = "Expense", ItemName = "Supplies (฿450.25)" }
        };

        var summary = AccountingRules.Summarize(month, 375.25m, 299m, records);

        Equal(month, summary.Month);
        Equal(1_260m, summary.ManualIncome);
        Equal(1_934.25m, summary.TotalIncome);
        Equal(450.25m, summary.Expenses);
        Equal(1_484m, summary.NetProfit);
        Equal(0, summary.UnreadableRecords);
    }),
    ("Empty and loss-making months have distinct results and malformed amounts are counted", () =>
    {
        var month = new DateOnly(2026, 11, 1);
        var records = new[]
        {
            new Booking { ServiceType = "Expense", ItemName = "Supplies (฿10.00)" },
            new Booking { ServiceType = "Income", ItemName = "Amount unknown" },
            new Booking { ServiceType = "Expense", ItemName = "Invalid (฿1,00.00)" },
            new Booking { ServiceType = "Expense", ItemName = "Not a cost (฿-5.00)" }
        };

        var empty = AccountingRules.Summarize(month, 0m, 0m, []);
        var loss = AccountingRules.Summarize(month, 0m, 0m, records);

        Equal(0m, empty.NetProfit);
        Equal(0, empty.UnreadableRecords);
        Equal(-10m, loss.NetProfit);
        Equal(3, loss.UnreadableRecords);
    }),
    ("Manual amounts use whole satang and new records use a stable number format", () =>
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var german = CultureInfo.GetCultureInfo("de-DE");
        var valid = new[] { 0.01m, 10_000_000m };
        var invalid = new[] { -1m, 0m, 1.001m, 10_000_000.01m };
        try
        {
            CultureInfo.CurrentCulture = german;
            var formatted = AccountingRules.FormatRecordName(" Rental ", 1_234.50m);
            var summary = AccountingRules.Summarize(new DateOnly(2026, 11, 1), 0m, 0m,
            [
                new Booking { ServiceType = "Income", ItemName = formatted },
                new Booking { ServiceType = "Expense", ItemName = "Legacy (฿1.234,50)" }
            ]);

            Equal("Rental (฿1,234.50)", formatted);
            Equal(1_234.50m, summary.ManualIncome);
            Equal(1_234.50m, summary.Expenses);
            Equal(0, summary.UnreadableRecords);
            Equal(true, valid.All(AccountingRules.IsValidAmount));
            Equal(false, invalid.Any(AccountingRules.IsValidAmount));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }),
    ("An approved table moves from the kitchen to payment and closure in order", () =>
    {
        var table = new Booking { ServiceType = "Table", Status = "QueueConfirmed" };

        var preparing = TableBookingWorkflow.NextForKitchen(table);
        var paymentBeforeServing = TableBookingWorkflow.NextForCashier(table);
        table.Status = preparing!;
        var served = TableBookingWorkflow.NextForKitchen(table);
        table.Status = served!;
        var paid = TableBookingWorkflow.NextForCashier(table);
        table.Status = paid!;
        var completed = TableBookingWorkflow.NextForCashier(table);
        table.Status = completed!;

        Equal("Preparing", preparing);
        Equal<string?>(null, paymentBeforeServing);
        Equal("Served", served);
        Equal("Paid", paid);
        Equal("Completed", completed);
        Equal<string?>(null, TableBookingWorkflow.NextForKitchen(table));
        Equal<string?>(null, TableBookingWorkflow.NextForCashier(table));
    }),
    ("Table workspaces show approved tables and exclude unapproved and unrelated bookings", () =>
    {
        var rows = new[]
        {
            new Booking { Id = 1, ServiceType = "Table", Status = "Pending" },
            new Booking { Id = 2, ServiceType = "Table", Status = "QueueConfirmed" },
            new Booking { Id = 3, ServiceType = "Table", Status = "Cancelled" },
            new Booking { Id = 4, ServiceType = "Photographer", Status = "QueueConfirmed" },
            new Booking { Id = 5, ServiceType = "Kitchen", Status = "Preparing" },
            new Booking { Id = 6, ServiceType = "Table", Status = "Completed" },
            new Booking { Id = 7, ServiceType = "Table", Status = "Unknown" }
        };

        var kitchenIds = rows.Where(TableBookingWorkflow.ForKitchen.Compile()).Select(row => row.Id);
        var cashierIds = rows.Where(TableBookingWorkflow.ForCashier.Compile()).Select(row => row.Id);

        Equal("2,5", string.Join(",", kitchenIds));
        Equal("2,6", string.Join(",", cashierIds));
    }),
    ("Legacy active states and manual kitchen orders still have the appropriate next action", () =>
    {
        var confirmedTable = new Booking { ServiceType = "Table", Status = "Confirmed" };
        var checkedInTable = new Booking { ServiceType = "Table", Status = "Checked-In" };
        var readyTable = new Booking { ServiceType = "Table", Status = "Ready" };
        var manualKitchenOrder = new Booking { ServiceType = "Kitchen", Status = "Preparing" };
        var unrelatedBooking = new Booking { ServiceType = "Promotion", Status = "Served" };

        var nextForKitchen = new[] { confirmedTable, checkedInTable, readyTable, manualKitchenOrder }
            .Select(TableBookingWorkflow.NextForKitchen);

        Equal("Preparing,Preparing,Served,Served", string.Join(",", nextForKitchen));
        Equal<string?>(null, TableBookingWorkflow.NextForCashier(confirmedTable));
        Equal<string?>(null, TableBookingWorkflow.NextForCashier(readyTable));
        Equal<string?>(null, TableBookingWorkflow.NextForCashier(manualKitchenOrder));
        Equal<string?>(null, TableBookingWorkflow.NextForCashier(unrelatedBooking));
    }),
    ("Table slots block intersections but allow adjacent bookings", () =>
    {
        var slot = TableReservationRules.Slots[2];

        Equal(true, TableReservationRules.Overlaps("12:00–14:00 น.", slot.Start, slot.End));
        Equal(true, TableReservationRules.Overlaps("13:30 - 14:15", slot.Start, slot.End));
        Equal(false, TableReservationRules.Overlaps("11:00 - 13:00 น.", slot.Start, slot.End));
        Equal(false, TableReservationRules.Overlaps("15:00 - 17:00 น.", slot.Start, slot.End));
        Equal(true, TableReservationRules.Overlaps("ไม่ระบุเวลา", slot.Start, slot.End));
    }),
    ("Reservation inputs resolve zone aliases and accept only defined slots", () =>
    {
        var zone = TableReservationRules.FindZone("Zone B: ชื่อที่เคยใช้ในฐานข้อมูล");
        var start = TableReservationRules.Slots[0].Start;
        var today = new DateOnly(2026, 11, 24);

        Equal("B", zone?.Key);
        Equal("bar", TableReservationRules.FindZone("BAR")?.Key);
        Equal<SeatingZone?>(null, TableReservationRules.FindZone("Zone X"));
        Equal<SeatingSlot?>(null, TableReservationRules.FindSlot("08:00 - 10:00 น."));
        Equal(true, TableReservationRules.HasStarted(today, start, today.ToDateTime(start)));
        Equal(false, TableReservationRules.HasStarted(today, start, today.ToDateTime(start).AddSeconds(-1)));
        Equal(false, TableReservationRules.TryDate("2026-02-30", out _));
        Equal(true, TableReservationRules.TryDate("2567-02-29", out var leapDay));
        Equal(new DateOnly(2024, 2, 29), leapDay);
        Equal("2567-02-29", TableReservationRules.LegacyDateAlias(leapDay));
    }),
    ("New registrations only accept the currently available Premium plan", () =>
    {
        var available = new[] { "Premium", "pReMiUm" };
        var unavailable = new[] { "Standard", "STANDARD", "invalid", "", null };

        var accepted = available.Select(MembershipRules.CanSelectSignupPackage);
        var rejected = unavailable.Select(MembershipRules.CanSelectSignupPackage);

        Equal(true, accepted.All(result => result));
        Equal(false, rejected.Any(result => result));
    }),
    ("Paid membership records the server price and a snapshot of the member name", () =>
    {
        var member = new User { Name = "Demo member", Package = "Premium" };
        var confirmedAt = new DateTime(2026, 11, 24, 6, 0, 0, DateTimeKind.Utc);

        var payment = MembershipRules.CreateSignupPayment(member, confirmedAt)!;
        member.Name = "Updated name";

        Equal(299m, payment.Amount);
        Equal("Premium", payment.Package);
        Equal("Demo member", payment.MemberName);
        Equal(confirmedAt, payment.PaidAt);
        Equal(member, payment.Member);
    }),
    ("Free and unrecognized packages never produce a signup payment", () =>
    {
        foreach (var package in new[] { "Standard", "unknown", "", null })
        {
            var member = new User { Package = package };

            var payment = MembershipRules.CreateSignupPayment(member, DateTime.UnixEpoch);

            Equal(true, payment is null);
        }
    }),
    ("Membership reports require an account and one of the three financial roles", () =>
    {
        const int accountId = 42;

        foreach (var role in new[] { "Accounting", "Admin", "owner" })
            Equal(true, MembershipRules.CanViewRevenue(accountId, role));
        foreach (var role in new[] { "User", "Guest", "Cashiers and staff", "Kitchen staff", "Staff queue", null })
            Equal(false, MembershipRules.CanViewRevenue(accountId, role));
        Equal(false, MembershipRules.CanViewRevenue(null, "Admin"));
        Equal(false, MembershipRules.CanViewRevenue(0, "Owner"));
    }),
    ("Membership revenue uses the Bangkok month across the UTC year boundary", () =>
    {
        var midnightInBangkok = new DateTime(2026, 12, 31, 17, 0, 0, DateTimeKind.Utc);

        var december = MembershipRules.BangkokMonthUtc(midnightInBangkok.AddTicks(-1));
        var january = MembershipRules.BangkokMonthUtc(midnightInBangkok);

        Equal(midnightInBangkok, december.End);
        Equal(midnightInBangkok, january.Start);
        Equal(new DateTime(2027, 1, 31, 17, 0, 0, DateTimeKind.Utc), january.End);
    }),
    ("Birthday billing applies the threshold, month, annual limit and preserves old discounts", () =>
    {
        var localTime = new DateTime(2026, 11, 24, 10, 0, 0);
        var birthday = new DateOnly(2000, 11, 2);
        var order = new FoodOrder { Subtotal = 1500m, Discount = 100m, DiscountCode = "Order1000" };

        Equal<decimal?>(225m, FoodPricing.BirthdayAtBilling(order, localTime, birthday, false));
        Equal<decimal?>(null, FoodPricing.BirthdayAtBilling(order, localTime, birthday, true));
        Equal<decimal?>(null, FoodPricing.BirthdayAtBilling(order, localTime.AddMonths(1), birthday, false));
        Equal<decimal?>(null, FoodPricing.BirthdayAtBilling(order, localTime, null, false));

        order.Subtotal = 1499.99m;
        Equal<decimal?>(null, FoodPricing.BirthdayAtBilling(order, localTime, birthday, false));
        order.Subtotal = 1500m;
        order.DiscountCode = FoodPricing.Birthday;
        Equal<decimal?>(null, FoodPricing.BirthdayAtBilling(order, localTime, birthday, false));
        order.DiscountCode = "other";
        order.Discount = 300m;
        Equal<decimal?>(null, FoodPricing.BirthdayAtBilling(order, localTime, birthday, false));
    }),
    ("Birthday requires the threshold and an unused entitlement", () =>
    {
        var morning = new DateTime(2026, 11, 24, 10, 0, 0);
        var under = FoodPricing.Discount(1499.99m, 0, morning, true);
        var exact = FoodPricing.Discount(1500m, 0, morning, true);
        var used = FoodPricing.Discount(1500m, 0, morning, false);
        Equal(100m, under.Amount);
        Equal((FoodPricing.Birthday, 225m), exact);
        Equal(("Order1000", 100m), used);
    }),
    ("Drink count includes exactly five, while four are ineligible", () =>
    {
        var morning = new DateTime(2026, 11, 24, 10, 0, 0);
        var below = FoodPricing.Discount(400m, 4, morning, false);
        var exact = FoodPricing.Discount(400m, 5, morning, false);
        Equal((null, 0m), below);
        Equal(("FiveDrinks", 50m), exact);
        Equal(true, FoodPricing.IsDrink("tea"));
        Equal(false, FoodPricing.IsDrink("bread"));
    }),
    ("Order-total discount applies at exactly 1,000 baht", () =>
    {
        var morning = new DateTime(2026, 11, 24, 10, 0, 0);
        var below = FoodPricing.Discount(999.99m, 0, morning, false);
        var exact = FoodPricing.Discount(1000m, 0, morning, false);
        Equal(0m, below.Amount);
        Equal(("Order1000", 100m), exact);
    }),
    ("Afternoon starts at 13:00 Bangkok time and ends at 16:00", () =>
    {
        var utc = new DateTime(2026, 11, 24, 6, 0, 0, DateTimeKind.Utc);
        var local = FoodPricing.BangkokTime(utc);
        Equal(0m, FoodPricing.Discount(80m, 0, local.AddTicks(-1), false).Amount);
        Equal(("Afternoon", 50m), FoodPricing.Discount(80m, 0, local, false));
        Equal(50m, FoodPricing.Discount(80m, 0, local.AddHours(3).AddTicks(-1), false).Amount);
        Equal(0m, FoodPricing.Discount(80m, 0, local.AddHours(3), false).Amount);
    }),
    ("Selects one best offer, rounds to satang, and never exceeds subtotal", () =>
    {
        var afternoon = new DateTime(2026, 11, 24, 13, 0, 0);
        var stackedCandidates = FoodPricing.Discount(1500.10m, 7, afternoon, true);
        var smallOrder = FoodPricing.Discount(35m, 0, afternoon, false);
        Equal((FoodPricing.Birthday, 225.02m), stackedCandidates);
        Equal(("Afternoon", 35m), smallOrder);
    }),
    ("Combo applies to the selected booking package only", () =>
    {
        var combo = new ServiceItem { Name = FoodPricing.ComboName, ServiceType = "Promotion", Price = 2490m };
        var another = new ServiceItem { Name = "Afternoon Tea", ServiceType = "Promotion", Price = 2490m };
        Equal(2190m, combo.Price - FoodPricing.ComboDiscount(combo));
        Equal(0m, FoodPricing.ComboDiscount(another));
    }),
    ("HTML date uses the Gregorian year and rejects a future or invalid birthday", () =>
    {
        var today = new DateOnly(2026, 11, 24);
        var parsed = FoodPricing.TryBirthDate("2000-11-01", today, out var birthday);
        Equal(true, parsed);
        Equal(new DateOnly(2000, 11, 1), birthday);
        Equal(false, FoodPricing.TryBirthDate("2026-11-25", today, out _));
        Equal(false, FoodPricing.TryBirthDate("2000-02-30", today, out _));
    }),
    ("Every staff role opens its own workspace and customer roles keep the storefront", () =>
    {
        Equal(("FoodStaff", "Cashier"), DestinationFor("Cashiers and staff"));
        Equal(("FoodStaff", "Kitchen"), DestinationFor("Kitchen staff"));
        Equal(("FoodStaff", "Accounting"), DestinationFor("Accounting"));
        Equal(("StaffQueue", "Index"), DestinationFor("Staff queue"));
        Equal(("Owner", "Index"), DestinationFor("Owner"));
        Equal<StaffWorkspace?>(null, StaffWorkspace.ForRole("User"));
        Equal<StaffWorkspace?>(null, StaffWorkspace.ForRole("Guest"));
        Equal<StaffWorkspace?>(null, StaffWorkspace.ForRole("Admin"));
    }),
    ("The cashier, kitchen and accounting only show the statuses they own", () =>
    {
        var orders = Enum.GetValues<FoodOrderStatus>().Select(status => new FoodOrder { Status = status }).ToList();
        var cashier = FoodOrderSections.ForStaff(new FoodStaffViewModel { Workspace = "Cashier", Orders = orders });
        var kitchen = FoodOrderSections.ForStaff(new FoodStaffViewModel { Workspace = "Kitchen", Orders = orders });
        var accounting = FoodOrderSections.ForStaff(new FoodStaffViewModel { Workspace = "Accounting", Orders = orders });
        Equal("Submitted,Ready", VisibleStatuses(cashier));
        Equal("InKitchen", VisibleStatuses(kitchen));
        Equal("AwaitingPayment,Paid", VisibleStatuses(accounting));
    }),
    ("Customer order and bill sections correctly handle mixed and empty histories", () =>
    {
        var orders = new[] { FoodOrderStatus.InKitchen, FoodOrderStatus.Served, FoodOrderStatus.AwaitingPayment, FoodOrderStatus.Paid }
            .Select(status => new FoodOrder { Status = status }).ToList();
        var tracking = FoodOrderSections.ForCustomer(new FoodOrderListViewModel { Orders = orders });
        var bills = FoodOrderSections.ForCustomer(new FoodOrderListViewModel { Bills = true, Orders = orders });
        var empty = FoodOrderSections.ForCustomer(new FoodOrderListViewModel { Bills = true });
        Equal("InKitchen,Served,AwaitingPayment,Paid", VisibleStatuses(tracking));
        Equal("Served,AwaitingPayment,Paid", VisibleStatuses(bills));
        Equal(3, empty.Count);
        Equal(true, empty.All(section => section.Orders.Count == 0));
    }),
    ("Booking queue includes reservations, and excludes manual kitchen and accounting records", () =>
    {
        var inQueue = BookingQueue.AppliesTo.Compile();
        Equal(true, inQueue(new Booking { ServiceType = "Table" }));
        Equal(true, inQueue(new Booking { ServiceType = "Promotion" }));
        Equal(false, inQueue(new Booking { ServiceType = "Kitchen" }));
        Equal(false, inQueue(new Booking { ServiceType = "Expense" }));
        Equal(false, BookingQueue.AllowsWalkIn("Expense"));
        Equal(true, BookingQueue.IsQueueDecision("QueueConfirmed"));
        Equal(false, BookingQueue.IsQueueDecision("Paid"));
    }),
    ("Admin booking counts and rows use the same grouping, including older statuses", () =>
    {
        var bookings = new[]
        {
            new Booking { Id = 1, Status = "Cancelled" }, new Booking { Id = 2, Status = "Pending" },
            new Booking { Id = 3, Status = "QueueConfirmed" }, new Booking { Id = 4, Status = "Served" },
            new Booking { Id = 5, Status = "Paid" }, new Booking { Id = 6, Status = "Unknown" }
        };
        var view = new AdminBookingBoardViewModel(bookings, "active");
        Equal("3,4", string.Join(',', view.Rows.Select(row => row.Booking.Id)));
        Equal(2, view.Filters.Single(item => item.Key == "active").Count);
        Equal(1, view.Filters.Single(item => item.Key == "cancelled").Count);
        Equal(1, view.Filters.Single(item => item.Key == "other").Count);
        Equal("all", new AdminBookingBoardViewModel(bookings, "bogus").SelectedFilter);
        Equal(0, new AdminBookingBoardViewModel([], "completed").Rows.Count);
    }),
    ("Admin booking transitions stop invalid and final edits, including cancelling a paid booking", () =>
    {
        Equal(true, AdminBookingBoardViewModel.CanChange("Pending", "Confirmed"));
        Equal(true, AdminBookingBoardViewModel.CanChange("Pending", "Cancelled"));
        Equal(false, AdminBookingBoardViewModel.CanChange("Pending", "Completed"));
        Equal(true, AdminBookingBoardViewModel.CanChange("Confirmed", "Completed"));
        Equal(true, AdminBookingBoardViewModel.CanChange("Paid", "Completed"));
        Equal(false, AdminBookingBoardViewModel.CanChange("Paid", "Cancelled"));
        Equal(false, AdminBookingBoardViewModel.CanChange("Completed", "Cancelled"));
        Equal(false, AdminBookingBoardViewModel.CanChange("Cancelled", "Confirmed"));
        Equal(false, AdminBookingBoardViewModel.CanChange("Unknown", "Confirmed"));
        Equal(false, AdminBookingBoardViewModel.CanChange("Pending", "MadeUp"));
        Equal("รออนุมัติ", AdminBookingBoardViewModel.Describe("Pending").Label);
        Equal("ไม่ระบุสถานะ", AdminBookingBoardViewModel.Describe(null).Label);
    }),
    ("Workflow determines every allowed role and expected source status", () =>
    {
        var expected = new[]
        {
            (FoodOrderStatus.Submitted, FoodOrderStatus.InKitchen, FoodOrderRules.Cashier),
            (FoodOrderStatus.InKitchen, FoodOrderStatus.Ready, FoodOrderRules.Kitchen),
            (FoodOrderStatus.Ready, FoodOrderStatus.Served, FoodOrderRules.Cashier),
            (FoodOrderStatus.Served, FoodOrderStatus.AwaitingPayment, "User"),
            (FoodOrderStatus.AwaitingPayment, FoodOrderStatus.Paid, FoodOrderRules.Accounting)
        };
        var actual = Enum.GetValues<FoodOrderAction>().Select(FoodOrderRules.Transition).ToArray();
        Equal(true, expected.SequenceEqual(actual));
        Equal(false, FoodOrderRules.IsStaff(FoodOrderRules.Cashier, FoodOrderRules.Accounting));
        Equal(true, FoodOrderRules.IsStaff("Admin", FoodOrderRules.Accounting));
        Equal(false, FoodOrderRules.CanOrder("Guest"));
    })
};

var failed = 0;
foreach (var (name, run) in checks)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception error)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {error.Message}");
    }
}
Console.WriteLine($"Tests: {checks.Length} | Passed: {checks.Length - failed} | Failed: {failed} | Skipped: 0");
return failed == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}

static (string Controller, string Action) DestinationFor(string role)
{
    var workspace = StaffWorkspace.ForRole(role)!;
    return (workspace.Controller, workspace.Action);
}

static string VisibleStatuses(IReadOnlyList<FoodOrderSection> sections) =>
    string.Join(',', sections.SelectMany(section => section.Orders).Select(order => order.Status));
