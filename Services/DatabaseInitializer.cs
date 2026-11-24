using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.Services;

internal sealed class DatabaseInitializer(Csi402dbContext db, IPasswordHasher<User> hasher)
{
    private static readonly string[] RequiredRoles =
    [
        "User", "Admin", "Cashiers and staff", "Kitchen staff", "Staff queue", "Guest", "Accounting", "Owner"
    ];

    public async Task InitializeAsync(string? demoPassword)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await CheckTablesAsync();

            // One MySQL session holds this lock while checking and adding seed data.
            // This prevents two instances from seeding the same database at once.
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT GET_LOCK(SHA2(CONCAT(DATABASE(), ':cafe-startup'), 256), 10)";
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1)
                throw new InvalidOperationException("Could not acquire the database initialization lock.");

            try
            {
                await SeedAsync(demoPassword);
            }
            finally
            {
                command.CommandText = "SELECT RELEASE_LOCK(SHA2(CONCAT(DATABASE(), ':cafe-startup'), 256))";
                await command.ExecuteScalarAsync();
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task CheckTablesAsync()
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()";
        await using var reader = await command.ExecuteReaderAsync();
        var existing = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
            existing.Add(reader.GetString(0));

        var missing = db.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .OfType<string>()
            .Distinct()
            .Where(table => !existing.Contains(table))
            .Order()
            .ToArray();

        if (missing.Length > 0)
            throw new InvalidOperationException($"Missing database tables: {string.Join(", ", missing)}. See README.md for setup.");
    }

    private async Task SeedAsync(string? demoPassword)
    {
        var roles = (await db.Roles.ToListAsync()).ToDictionary(role => role.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in RequiredRoles)
        {
            if (roles.ContainsKey(name))
                continue;

            var role = new Role { Name = name };
            roles.Add(name, role);
            db.Roles.Add(role);
        }

        if (!await db.ServiceItems.AnyAsync())
            db.ServiceItems.AddRange(DefaultServices.Create());

        if (demoPassword is not null)
        {
            foreach (var account in DemoAccountSeed.Accounts)
            {
                if (await db.Users.AnyAsync(user => user.Username == account.Username || user.Email == account.Email))
                    continue;

                db.Staff.Add(account.CreateStaff(roles[account.RoleName], hasher, demoPassword));
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();
    }
}
