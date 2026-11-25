using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.Services.Membership;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection. See README.md.");

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<FoodOrderService>();
builder.Services.AddScoped<Nattapong_7379_L002.Services.TableReservations>();
builder.Services.AddScoped<MembershipRevenue>();
builder.Services.AddScoped<MonthlyProfit>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddDbContext<Csi402dbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
