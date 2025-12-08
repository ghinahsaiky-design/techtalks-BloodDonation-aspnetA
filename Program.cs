using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // <-- Add this using

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register BloodDonationContext with SQL Server connection
builder.Services.AddDbContext<BloodDonationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloodDonationDb")));

builder.Services.AddIdentity<Users, IdentityRole<int>>()
    .AddEntityFrameworkStores<BloodDonationContext>()
    .AddDefaultTokenProviders();


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

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
