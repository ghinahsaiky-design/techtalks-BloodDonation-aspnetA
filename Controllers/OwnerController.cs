using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using BloodDonation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonation.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BloodDonation.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly BloodDonationContext _context;

        public OwnerController(UserManager<Users> userManager, BloodDonationContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private async Task<bool> IsOwnerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user != null && user.Role == "Owner";
        }

        // OwnerOverview
        public async Task<IActionResult> Index()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.OwnerName = user.FirstName + " " + user.LastName;

            // Gather Statistics
            var totalUsers = await _context.Users.CountAsync();
            var totalDonations = await _context.DonorConfirmations.CountAsync(); 
            
            var roleDistribution = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Role, v => v.Count);

            // Growth Calculations (vs Last Month)
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            
            var usersLastMonthTotal = await _context.Users.CountAsync(u => u.CreatedAt < oneMonthAgo);
            var userGrowth = usersLastMonthTotal > 0 
                ? ((double)(totalUsers - usersLastMonthTotal) / usersLastMonthTotal) * 100 
                : 100;

            var donationsLastMonthTotal = await _context.DonorConfirmations.CountAsync(d => d.ConfirmedAt < oneMonthAgo);
            var donationGrowth = donationsLastMonthTotal > 0
                ? ((double)(totalDonations - donationsLastMonthTotal) / donationsLastMonthTotal) * 100
                : 100;

            // Monthly Trends (Last 6 Months)
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddMonths(-5); // Last 6 months including current
            startDate = new DateTime(startDate.Year, startDate.Month, 1); // Start from the 1st of that month
            
            var monthlyDonationsData = await _context.DonorConfirmations
                .Where(d => d.ConfirmedAt >= startDate)
                .GroupBy(d => new { d.ConfirmedAt.Year, d.ConfirmedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyRegistrationsData = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyRequestsData = await _context.DonorRequests
                .Where(r => r.CreatedAt >= startDate)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var dailyDonations = new List<int>();
            var dailyRegistrations = new List<int>();
            var dailyRequests = new List<int>();
            var daysLabels = new List<string>();

            for (int i = 0; i < 6; i++)
            {
                var d = startDate.AddMonths(i);
                daysLabels.Add(d.ToString("MMM")); // Jan, Feb, etc.
                
                dailyDonations.Add(monthlyDonationsData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0);
                dailyRegistrations.Add(monthlyRegistrationsData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0);
                dailyRequests.Add(monthlyRequestsData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0);
            }

            // Recent Admin Actions
            var recentActions = await _context.Actions
                .Include(a => a.PerformedByUser)
                .Where(a => a.PerformedByUser.Role == "Admin")
                .OrderByDescending(a => a.PerformedAt)
                .Take(5)
                .ToListAsync();

            var newHospitalsThisMonth = await _context.Users
                .CountAsync(u => u.Role == "Hospital" && u.CreatedAt >= oneMonthAgo);

            var viewModel = new OwnerDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalDonations = totalDonations,
                TotalAdmins = roleDistribution.ContainsKey("Admin") ? roleDistribution["Admin"] : 0,
                TotalDonors = roleDistribution.ContainsKey("Donor") ? roleDistribution["Donor"] : 0,
                TotalHospitals = roleDistribution.ContainsKey("Hospital") ? roleDistribution["Hospital"] : 0,
                TotalOwners = roleDistribution.ContainsKey("Owner") ? roleDistribution["Owner"] : 0,
                NewHospitalsThisMonth = newHospitalsThisMonth,
                RoleDistribution = roleDistribution,
                UserGrowthPercentage = Math.Round(userGrowth, 1),
                DonationGrowthPercentage = Math.Round(donationGrowth, 1),
                DailyDonations = dailyDonations,
                DailyRegistrations = dailyRegistrations,
                DailyRequests = dailyRequests,
                DaysLabels = daysLabels,
                RecentActions = recentActions
            };

            return View(viewModel);
        }

        // Admins tab
        public async Task<IActionResult> AdminManagement(string searchString, string status)
        {
            if (!await IsOwnerAsync())
                return Forbid();

            var query = _context.Users.Where(u => u.Role == "Admin");

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FirstName.Contains(searchString) || 
                                         u.LastName.Contains(searchString) || 
                                         u.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(u => u.Status == status);
            }

            var admins = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var viewModel = new AdminListViewModel
            {
                Admins = admins,
                TotalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin"),
                ActiveAdmins = await _context.Users.CountAsync(u => u.Role == "Admin" && u.Status == "Active"), 
                PendingAdmins = await _context.Users.CountAsync(u => u.Role == "Admin" && u.Status == "Pending"),
                InactiveAdmins = await _context.Users.CountAsync(u => u.Role == "Admin" && u.Status == "Inactive")
            };

            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentStatus = status;

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (!await IsOwnerAsync()) return Forbid();

            if (ModelState.IsValid)
            {
                var user = new Users
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    _context.Actions.Add(new TrackedAction
                    {
                        Name = "Create Admin",
                        Description = $"Created new admin: {user.Email}",
                        Type = ActionType.Create,
                        PerformedByUserId = currentUser.Id,
                        PerformedAt = DateTime.UtcNow,
                        TargetUserId = user.Id
                    });
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(AdminManagement));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditAdmin(int id)
        {
             if (!await IsOwnerAsync()) return Forbid();
             
             var user = await _userManager.FindByIdAsync(id.ToString());
             if (user == null || user.Role != "Admin") return NotFound();

             var model = new EditAdminViewModel
             {
                 Id = user.Id,
                 FirstName = user.FirstName,
                 LastName = user.LastName,
                 Email = user.Email
             };
             return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditAdmin(EditAdminViewModel model)
        {
            if (!await IsOwnerAsync()) return Forbid();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id.ToString());
                if (user == null) return NotFound();

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    _context.Actions.Add(new TrackedAction
                    {
                        Name = "Edit Admin",
                        Description = $"Updated admin details: {user.Email}",
                        Type = ActionType.Update,
                        PerformedByUserId = currentUser.Id,
                        PerformedAt = DateTime.UtcNow,
                        TargetUserId = user.Id
                    });
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction(nameof(AdminManagement));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            if (!await IsOwnerAsync()) return Forbid();

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null || user.Role != "Admin") return NotFound();

            var email = user.Email;
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                _context.Actions.Add(new TrackedAction
                {
                    Name = "Delete Admin",
                    Description = $"Deleted admin: {email}",
                    Type = ActionType.Delete,
                    PerformedByUserId = currentUser.Id,
                    PerformedAt = DateTime.UtcNow,
                    TargetUserId = null
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminManagement));
        }

        // Admin Actions tab
        public async Task<IActionResult> AdminActions()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            var actions = await _context.Actions
                .Include(a => a.PerformedByUser)
                .Where(a => a.PerformedByUser.Role == "Admin") // Filter for Admin actions
                .OrderByDescending(a => a.PerformedAt)
                .Take(100) // Limit to last 100 actions
                .ToListAsync();

            var viewModel = new AdminActionsViewModel
            {
                Actions = actions
            };

            return View(viewModel);
        }

        // Hospital tab
        public async Task<IActionResult> HospitalManagement(string q, string status, string location)
        {
            if (!await IsOwnerAsync())
                return Forbid();

            var query = _context.Hospitals
                .Include(h => h.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(h => h.Name.Contains(q) || 
                                         h.User.Email.Contains(q) || 
                                         h.Id.ToString().Contains(q));
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(h => h.User.Status == status);
            }

            if (!string.IsNullOrEmpty(location) && location != "All")
            {
                query = query.Where(h => h.City == location || h.State == location);
            }

            var hospitals = await query
                .OrderByDescending(h => h.User.CreatedAt)
                .ToListAsync();

            ViewBag.SearchQuery = q;
            ViewBag.CurrentStatus = status ?? "All";
            ViewBag.CurrentLocation = location ?? "All";
            
            // Get all locations from the database for the filter
            ViewBag.Locations = await _context.Locations
                .Select(l => l.Districts)
                .OrderBy(d => d)
                .ToListAsync();

            return View(hospitals);
        }

        public async Task<IActionResult> GenerateReport()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            // 1. Fetch KPI Data
            var totalUsers = await _context.Users.CountAsync();
            var totalDonations = await _context.DonorConfirmations.CountAsync();
            
            var roleDistribution = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Role, v => v.Count);

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var usersLastMonthTotal = await _context.Users.CountAsync(u => u.CreatedAt < oneMonthAgo);
            var userGrowth = usersLastMonthTotal > 0 
                ? ((double)(totalUsers - usersLastMonthTotal) / usersLastMonthTotal) * 100 
                : 100;

            var donationsLastMonthTotal = await _context.DonorConfirmations.CountAsync(d => d.ConfirmedAt < oneMonthAgo);
            var donationGrowth = donationsLastMonthTotal > 0
                ? ((double)(totalDonations - donationsLastMonthTotal) / donationsLastMonthTotal) * 100
                : 100;

            // 2. Fetch Monthly Trends (Last 6 Months)
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddMonths(-5);
            startDate = new DateTime(startDate.Year, startDate.Month, 1);
            
            var monthlyDonationsData = await _context.DonorConfirmations
                .Where(d => d.ConfirmedAt >= startDate)
                .GroupBy(d => new { d.ConfirmedAt.Year, d.ConfirmedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyRegistrationsData = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyRequestsData = await _context.DonorRequests
                .Where(r => r.CreatedAt >= startDate)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // 3. Fetch Recent Admin Actions
            var recentActions = await _context.Actions
                .Include(a => a.PerformedByUser)
                .OrderByDescending(a => a.PerformedAt)
                .Take(10)
                .ToListAsync();

            // 4. Fetch Recent Registrations
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();

            // 5. Fetch Top Donors
            var topDonors = await _context.DonorConfirmations
                .GroupBy(d => d.DonorId)
                .Select(g => new { DonorId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();
            
            var topDonorIds = topDonors.Select(x => x.DonorId).ToList();
            var topDonorProfiles = await _context.DonorProfile
                .Include(dp => dp.User)
                .Where(dp => topDonorIds.Contains(dp.DonorId))
                .ToListAsync();

            // QuestPDF Setup
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("BloodConnect").FontSize(20).SemiBold().FontColor(Colors.Red.Medium);
                                c.Item().Text("Owner Dashboard Report").FontSize(14).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("MMM dd, yyyy")).FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(15);

                        // Section 1: Executive Summary (KPIs)
                        col.Item().Text("Executive Summary").FontSize(14).SemiBold().FontColor(Colors.Black);
                        col.Item().Row(row =>
                        {
                            row.Spacing(10);
                            
                            // KPI Card Helper
                            void KpiCard(IContainer c, string title, string value, string subtext, string colorHex)
                            {
                                c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
                                {
                                    column.Item().Text(title).FontSize(9).FontColor(Colors.Grey.Darken1);
                                    column.Item().Text(value).FontSize(18).SemiBold().FontColor(colorHex);
                                    column.Item().Text(subtext).FontSize(8).FontColor(Colors.Grey.Medium);
                                });
                            }

                            row.RelativeItem().Element(c => KpiCard(c, "Total Users", totalUsers.ToString("N0"), $"{userGrowth:+0.0;-0.0}% vs last month", Colors.Blue.Medium));
                            row.RelativeItem().Element(c => KpiCard(c, "Total Donations", totalDonations.ToString("N0"), $"{donationGrowth:+0.0;-0.0}% vs last month", Colors.Red.Medium));
                            row.RelativeItem().Element(c => KpiCard(c, "Active Hospitals", (roleDistribution.ContainsKey("Hospital") ? roleDistribution["Hospital"] : 0).ToString(), "Registered Partners", Colors.Purple.Medium));
                            row.RelativeItem().Element(c => KpiCard(c, "System Stock", "88%", "Status: Healthy", Colors.Green.Medium));
                        });

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten3);

                        // Section 2: Role Distribution & Daily Trends
                        col.Item().Row(row =>
                        {
                            row.Spacing(20);

                            // Role Distribution Table
                            row.RelativeItem(1).Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text("User Distribution").FontSize(12).SemiBold();
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(50);
                                    });
                                    
                                    table.Header(h =>
                                    {
                                        h.Cell().Text("Role").SemiBold();
                                        h.Cell().AlignRight().Text("Count").SemiBold();
                                    });

                                    var allRoles = new[] { "Donor", "Hospital", "Admin", "Owner" };
                                    foreach (var roleName in allRoles)
                                    {
                                        var count = roleDistribution.ContainsKey(roleName) ? roleDistribution[roleName] : 0;
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(roleName);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(count.ToString());
                                    }
                                });
                            });

                            // Monthly Trends Table
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text("Monthly Activity (Last 6 Months)").FontSize(12).SemiBold();
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Text("Month").SemiBold();
                                        h.Cell().AlignRight().Text("Donations").SemiBold();
                                        h.Cell().AlignRight().Text("Registers").SemiBold();
                                        h.Cell().AlignRight().Text("Requests").SemiBold();
                                    });

                                    for (int i = 0; i < 6; i++)
                                    {
                                        var d = startDate.AddMonths(i);
                                        var donations = monthlyDonationsData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0;
                                        var registrations = monthlyRegistrationsData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0;
                                        var requests = monthlyRequestsData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0;

                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(d.ToString("MMM yyyy"));
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(donations.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(registrations.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(requests.ToString());
                                    }
                                });
                            });
                        });

                        col.Item().PageBreak();

                        // Section 3: Recent Activity
                        col.Item().Text("Recent System Activity").FontSize(14).SemiBold().FontColor(Colors.Black);
                        
                        col.Item().Row(row =>
                        {
                            row.Spacing(20);

                            // Recent Registrations
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text("New Users").FontSize(12).SemiBold();
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Text("Name").SemiBold();
                                        h.Cell().Text("Role").SemiBold();
                                        h.Cell().AlignRight().Text("Date").SemiBold();
                                    });

                                    foreach (var user in recentUsers)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text($"{user.FirstName} {user.LastName}");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(user.Role);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(user.CreatedAt.ToString("MMM dd"));
                                    }
                                });
                            });

                            // Top Donors
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text("Top Donors").FontSize(12).SemiBold();
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Text("Donor Name").SemiBold();
                                        h.Cell().AlignRight().Text("Donations").SemiBold();
                                    });

                                    foreach (var donorStat in topDonors)
                                    {
                                        var profile = topDonorProfiles.FirstOrDefault(p => p.DonorId == donorStat.DonorId);
                                        var name = profile != null ? $"{profile.User.FirstName} {profile.User.LastName}" : $"Donor #{donorStat.DonorId}";
                                        
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(name);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(donorStat.Count.ToString());
                                    }
                                });
                            });
                        });

                        col.Item().PaddingTop(10);

                        // Recent Admin Actions
                        col.Item().PaddingBottom(5).Text("Recent Admin Actions").FontSize(12).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Action").SemiBold();
                                h.Cell().Text("Admin").SemiBold();
                                h.Cell().Text("Details").SemiBold();
                                h.Cell().AlignRight().Text("Time").SemiBold();
                            });

                            foreach (var action in recentActions)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(action.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text($"{action.PerformedByUser.FirstName} {action.PerformedByUser.LastName}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(action.Description ?? "-");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(action.PerformedAt.ToString("MMM dd HH:mm"));
                            }
                        });

                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream, "application/pdf", $"BloodConnect_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }

    }
}
