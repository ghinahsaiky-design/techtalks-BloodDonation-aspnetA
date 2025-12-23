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

// Get database configuration
var useMySQL = builder.Configuration.GetValue<bool>("UseMySQL", true);
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? (useMySQL ? "MySQL" : "SQLServer");
var connectionString = useMySQL 
    ? builder.Configuration.GetConnectionString("BloodDonationDb") 
    : builder.Configuration.GetConnectionString("BloodDonationDb_SqlServer") ?? builder.Configuration.GetConnectionString("BloodDonationDb");

// Register BloodDonationContext with selected database provider
builder.Services.AddDbContext<BloodDonationContext>(options =>
{
    if (useMySQL || databaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
    {
        // Use MySQL 8.0.33 server version (or specify your MySQL version)
        // You can also use ServerVersion.AutoDetect(connectionString) if MySQL is running
        var serverVersion = ServerVersion.Create(8, 0, 33, ServerType.MySql);
        options.UseMySql(connectionString, serverVersion, mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
        });
    }
    else if (databaseProvider.Equals("SQLServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException($"Unsupported database provider: {databaseProvider}. Supported providers: MySQL, SQLServer");
    }
});

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
