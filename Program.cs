using BloodDonation.Data;
using BloodDonation.Models;
using BloodDonation.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure database provider based on settings
var useMySQL = builder.Configuration.GetValue<bool>("UseMySQL");
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider");

if (useMySQL || databaseProvider == "MySQL")
{
    var connectionString = builder.Configuration.GetConnectionString("BloodDonationDb");
    builder.Services.AddDbContext<BloodDonationContext>(options =>
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("BloodDonationDb_SqlServer");
    builder.Services.AddDbContext<BloodDonationContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}

builder.Services.AddIdentity<Users, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
})
    .AddEntityFrameworkStores<BloodDonationContext>()
    .AddDefaultTokenProviders();

// Register DataSeeder as scoped service
builder.Services.AddScoped<DataSeeder>();

// Register NotificationService as scoped service
builder.Services.AddScoped<NotificationService>();

// Register EmailMonitoringService as hosted service (background service)
builder.Services.AddHostedService<EmailMonitoringService>();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";       // Redirect to login page
    options.LogoutPath = "/Auth/Logout";     // Redirect after logout
    //options.AccessDeniedPath = "/Auth/AccessDenied"; // Optional: page for unauthorized access
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

// Build app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // 30 days HSTS
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Seed admin user and demo data using scoped service
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedOwnerUserAsync();
    await seeder.SeedDemoDataAsync();
}

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
