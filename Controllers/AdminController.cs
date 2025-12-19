using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BloodDonation.Data;
using BloodDonation.Models;
using BloodDonation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BloodDonation.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly BloodDonationContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly NotificationService _notificationService;

        public AdminController(
            BloodDonationContext context, 
            UserManager<Users> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // Helper method to check if current user is admin
        private async Task<bool> IsAdminAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return false;

            var user = await _userManager.GetUserAsync(User);
            return user != null && (user.Role == "Admin" || user.Role == "Owner");
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index()
        {
            // Check if user is admin
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Calculate statistics
            var totalDonors = await _context.DonorProfile.CountAsync();
            
            // Count successful donations (donors who have donated - LastDonationDate is not null)
            var successfulDonations = await _context.DonorProfile
                .Where(d => d.LastDonationDate.HasValue)
                .CountAsync();

            // Count active requests (from DonorRequest model)
            var activeRequests = await _context.DonorRequests
                .Where(r => r.Status != "Completed" && r.Status != "Cancelled" && 
                           r.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            // Calculate blood stock levels based on available healthy donors
            var totalHealthyDonors = await _context.DonorProfile
                .Where(d => d.IsHealthyForDonation && d.IsAvailable)
                .CountAsync();
            
            var bloodStockPercentage = totalDonors > 0 
                ? (int)((double)totalHealthyDonors / totalDonors * 100) 
                : 0;

            // Get recent donors who registered (last 10, ordered by registration date)
            var recentDonorsList = await _context.DonorProfile
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .Include(d => d.Location)
                .OrderByDescending(d => d.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentDonors = recentDonorsList.Select(d => new RecentDonorViewModel
            {
                DonorName = d.IsIdentityHidden 
                    ? $"Donor #{d.DonorId}" 
                    : (d.User != null ? d.User.FirstName + " " + d.User.LastName : "Unknown"),
                BloodType = d.BloodType?.Type ?? "Unknown",
                Date = d.CreatedAt,
                Status = d.IsAvailable && d.IsHealthyForDonation ? "Available" : "Unavailable",
                Location = d.Location?.Districts ?? "Unknown",
                HasDonated = d.LastDonationDate.HasValue
            }).ToList();

            // Calculate blood type distribution
            var bloodTypeDistribution = await _context.DonorProfile
                .GroupBy(d => d.BloodType.Type)
                .Select(g => new BloodTypeDistributionViewModel
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Percentage = totalDonors > 0 ? (double)g.Count() / totalDonors * 100 : 0
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // Calculate donation trends (last 6 months)
            var donationTrends = new List<DonationTrendViewModel>();
            var now = DateTime.UtcNow;
            
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                
                // Count donors who made donations in this month
                // Note: This counts unique donors per month, not total donation events
                var count = await _context.DonorProfile
                    .Where(d => d.LastDonationDate.HasValue && 
                               d.LastDonationDate.Value >= monthStart && 
                               d.LastDonationDate.Value < monthEnd)
                    .CountAsync();
                
                donationTrends.Add(new DonationTrendViewModel
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    MonthShort = monthStart.ToString("MMM"),
                    Count = count
                });
            }

            // Pass data to view using ViewBag for backward compatibility
            ViewBag.TotalDonors = totalDonors;
            ViewBag.SuccessfulDonations = successfulDonations;
            ViewBag.ActiveRequests = activeRequests;
            ViewBag.BloodStockPercentage = bloodStockPercentage;
            ViewBag.RecentDonations = recentDonors;
            ViewBag.BloodTypeDistribution = bloodTypeDistribution;
            ViewBag.DonationTrends = donationTrends;
            ViewBag.TotalHealthyDonors = totalHealthyDonors;
            ViewBag.AdminName = user.FirstName + " " + user.LastName;

            return View();
        }

        // GET: Admin/ExportDashboardReport
        public async Task<IActionResult> ExportDashboardReport()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            // Calculate all statistics (same as Index action)
            var totalDonors = await _context.DonorProfile.CountAsync();
            var successfulDonations = await _context.DonorProfile
                .Where(d => d.LastDonationDate.HasValue)
                .CountAsync();
            var activeRequests = await _context.DonorRequests
                .Where(r => r.Status != "Completed" && r.Status != "Cancelled" && 
                           r.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();
            var totalHealthyDonors = await _context.DonorProfile
                .Where(d => d.IsHealthyForDonation && d.IsAvailable)
                .CountAsync();
            var bloodStockPercentage = totalDonors > 0 
                ? (int)((double)totalHealthyDonors / totalDonors * 100) 
                : 0;

            // Get recent donors
            var recentDonorsList = await _context.DonorProfile
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .Include(d => d.Location)
                .OrderByDescending(d => d.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Get blood type distribution
            var bloodTypeDistribution = await _context.DonorProfile
                .GroupBy(d => d.BloodType.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Percentage = totalDonors > 0 ? (double)g.Count() / totalDonors * 100 : 0
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // Get donation trends
            var donationTrends = new List<dynamic>();
            var now = DateTime.UtcNow;
            
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                
                var count = await _context.DonorProfile
                    .Where(d => d.LastDonationDate.HasValue && 
                               d.LastDonationDate.Value >= monthStart && 
                               d.LastDonationDate.Value < monthEnd)
                    .CountAsync();
                
                donationTrends.Add(new
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Count = count
                });
            }

            // Set license for QuestPDF (free for non-commercial use)
            QuestPDF.Settings.License = LicenseType.Community;

            // Generate PDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("BloodConnect")
                                    .FontSize(24)
                                    .FontColor(Colors.Red.Darken2)
                                    .Bold();
                                column.Item().Text("Admin Dashboard Report")
                                    .FontSize(14)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(100).AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Summary Statistics Section
                            column.Item().PaddingBottom(10).Column(statsColumn =>
                            {
                                statsColumn.Item().PaddingBottom(5).Text("SUMMARY STATISTICS")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Red.Darken2);
                                
                                statsColumn.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Metric").Bold();
                                        header.Cell().Element(CellStyle).AlignRight().Text("Value").Bold();
                                    });

                                    table.Cell().Element(CellStyle).Text("Total Donors");
                                    table.Cell().Element(CellStyle).AlignRight().Text(totalDonors.ToString());

                                    table.Cell().Element(CellStyle).Text("Successful Donations");
                                    table.Cell().Element(CellStyle).AlignRight().Text(successfulDonations.ToString());

                                    table.Cell().Element(CellStyle).Text("Active Requests (Last 30 Days)");
                                    table.Cell().Element(CellStyle).AlignRight().Text(activeRequests.ToString());

                                    table.Cell().Element(CellStyle).Text("Total Healthy & Available Donors");
                                    table.Cell().Element(CellStyle).AlignRight().Text(totalHealthyDonors.ToString());

                                    table.Cell().Element(CellStyle).Text("Blood Stock Percentage");
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{bloodStockPercentage}%");
                                });
                            });

                            // Blood Type Distribution Section
                            column.Item().PaddingBottom(10).Column(distColumn =>
                            {
                                distColumn.Item().PaddingBottom(5).Text("BLOOD TYPE DISTRIBUTION")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Red.Darken2);

                                distColumn.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Blood Type").Bold();
                                        header.Cell().Element(CellStyle).AlignRight().Text("Count").Bold();
                                        header.Cell().Element(CellStyle).AlignRight().Text("Percentage").Bold();
                                    });

                                    foreach (var dist in bloodTypeDistribution)
                                    {
                                        table.Cell().Element(CellStyle).Text(dist.Type);
                                        table.Cell().Element(CellStyle).AlignRight().Text(dist.Count.ToString());
                                        table.Cell().Element(CellStyle).AlignRight().Text($"{dist.Percentage:F2}%");
                                    }
                                });
                            });

                            // Donation Trends Section
                            column.Item().PaddingBottom(10).Column(trendsColumn =>
                            {
                                trendsColumn.Item().PaddingBottom(5).Text("DONATION TRENDS (Last 6 Months)")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Red.Darken2);

                                trendsColumn.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Month").Bold();
                                        header.Cell().Element(CellStyle).AlignRight().Text("Donations").Bold();
                                    });

                                    foreach (dynamic trend in donationTrends)
                                    {
                                        string month = trend.Month?.ToString() ?? "N/A";
                                        int count = (int)trend.Count;
                                        table.Cell().Element(CellStyle).Text(month);
                                        table.Cell().Element(CellStyle).AlignRight().Text(count.ToString());
                                    }
                                });
                            });

                            // Recent Donors Section
                            column.Item().PaddingBottom(10).Column(donorsColumn =>
                            {
                                donorsColumn.Item().PaddingBottom(5).Text("RECENT DONORS (Last 10 Registrations)")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Red.Darken2);

                                donorsColumn.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("ID").Bold();
                                        header.Cell().Element(CellStyle).Text("Name").Bold();
                                        header.Cell().Element(CellStyle).Text("Email").Bold();
                                        header.Cell().Element(CellStyle).Text("Blood Type").Bold();
                                        header.Cell().Element(CellStyle).Text("Location").Bold();
                                        header.Cell().Element(CellStyle).Text("Status").Bold();
                                        header.Cell().Element(CellStyle).Text("Has Donated").Bold();
                                    });

                                    foreach (var donor in recentDonorsList)
                                    {
                                        var donorName = donor.IsIdentityHidden 
                                            ? $"Donor #{donor.DonorId}" 
                                            : (donor.User != null ? $"{donor.User.FirstName} {donor.User.LastName}" : "Unknown");
                                        var email = donor.User?.Email ?? "N/A";
                                        var bloodType = donor.BloodType?.Type ?? "Unknown";
                                        var location = donor.Location?.Districts ?? "Unknown";
                                        var status = donor.IsAvailable && donor.IsHealthyForDonation ? "Available" : "Unavailable";
                                        var registrationDate = donor.CreatedAt.ToString("yyyy-MM-dd");
                                        var hasDonated = donor.LastDonationDate.HasValue ? "Yes" : "No";

                                        table.Cell().Element(CellStyle).Text(donor.DonorId.ToString());
                                        table.Cell().Element(CellStyle).Text(donorName);
                                        table.Cell().Element(CellStyle).Text(email).FontSize(8);
                                        table.Cell().Element(CellStyle).Text(bloodType);
                                        table.Cell().Element(CellStyle).Text(location);
                                        table.Cell().Element(CellStyle).Text(status);
                                        table.Cell().Element(CellStyle).Text(hasDonated);
                                    }
                                });
                            });
                        });
                });
            })
            .GeneratePdf();

            var fileName = $"BloodConnect_Dashboard_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Helper method for table cell styling
        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(5);
        }

        // GET: Admin/DonorManagement
        public async Task<IActionResult> DonorManagement(string searchTerm = "", string bloodTypeFilter = "", string statusFilter = "")
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Build query
            var query = _context.DonorProfile
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .Include(d => d.Location)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => 
                    d.User.FirstName.Contains(searchTerm) || 
                    d.User.LastName.Contains(searchTerm) ||
                    d.User.Email.Contains(searchTerm) ||
                    d.DonorId.ToString().Contains(searchTerm));
            }

            // Apply blood type filter
            if (!string.IsNullOrEmpty(bloodTypeFilter) && bloodTypeFilter != "All Blood Types")
            {
                query = query.Where(d => d.BloodType.Type == bloodTypeFilter);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All Status")
            {
                if (statusFilter == "Available")
                {
                    query = query.Where(d => d.IsAvailable && d.IsHealthyForDonation);
                }
                else if (statusFilter == "Unavailable")
                {
                    query = query.Where(d => !d.IsAvailable || !d.IsHealthyForDonation);
                }
            }

            // Calculate statistics
            var totalDonors = await _context.DonorProfile.CountAsync();
            var availableDonors = await _context.DonorProfile
                .Where(d => d.IsAvailable && d.IsHealthyForDonation)
                .CountAsync();
            
            // Rare blood types (AB-, O-, B-, A-)
            var rareBloodTypes = new[] { "AB-", "O-", "B-", "A-" };
            var rareBloodTypeCount = await _context.DonorProfile
                .Where(d => rareBloodTypes.Contains(d.BloodType.Type))
                .CountAsync();

            // Top region by donor count
            var topRegion = await _context.DonorProfile
                .GroupBy(d => d.Location.Districts)
                .OrderByDescending(g => g.Count())
                .Select(g => new { District = g.Key, Count = g.Count() })
                .FirstOrDefaultAsync();

            // Get all donors with pagination
            var donors = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            // Pass data to view
            ViewBag.TotalDonors = totalDonors;
            ViewBag.AvailableDonors = availableDonors;
            ViewBag.RareBloodTypeCount = rareBloodTypeCount;
            ViewBag.TopRegion = topRegion?.District ?? "N/A";
            ViewBag.TopRegionCount = topRegion?.Count ?? 0;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.BloodTypeFilter = bloodTypeFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            ViewBag.AdminName = user.FirstName + " " + user.LastName;
            ViewBag.TotalCount = donors.Count;

            return View(donors);
        }

        // GET: Admin/RequestManagement
        public async Task<IActionResult> RequestManagement(string searchTerm = "", string bloodTypeFilter = "", string urgencyFilter = "", string statusFilter = "")
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Build query - using DonorRequest
            var query = _context.DonorRequests
                .Include(r => r.BloodType)
                .Include(r => r.Location)
                .Include(r => r.RequestedByUser)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => 
                    r.PatientName.Contains(searchTerm) ||
                    r.RequestId.ToString().Contains(searchTerm) ||
                    (r.HospitalName != null && r.HospitalName.Contains(searchTerm)) ||
                    r.ContactNumber.Contains(searchTerm));
            }

            // Apply blood type filter
            if (!string.IsNullOrEmpty(bloodTypeFilter))
            {
                query = query.Where(r => r.BloodType.Type == bloodTypeFilter);
            }

            // Apply urgency filter
            if (!string.IsNullOrEmpty(urgencyFilter))
            {
                query = query.Where(r => r.UrgencyLevel == urgencyFilter);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            // Get all requests
            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Calculate statistics
            var totalRequests = await _context.DonorRequests.CountAsync();
            
            // Pending requests
            var pendingRequests = await _context.DonorRequests
                .Where(r => r.Status == "Pending")
                .CountAsync();

            // Critical cases
            var criticalCases = await _context.DonorRequests
                .Where(r => r.UrgencyLevel == "Critical" && r.Status != "Completed")
                .CountAsync();

            // Pass data to view
            ViewBag.TotalRequests = totalRequests;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.CriticalCases = criticalCases;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.BloodTypeFilter = bloodTypeFilter;
            ViewBag.UrgencyFilter = urgencyFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
            ViewBag.AdminName = user.FirstName + " " + user.LastName;

            return View(requests);
        }

        // POST: Admin/CreateDonorRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDonorRequest(DonorRequest request)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    request.RequestedByUserId = user.Id;
                }

                request.Status = "Pending";
                request.CreatedAt = DateTime.UtcNow;

                _context.DonorRequests.Add(request);
                await _context.SaveChangesAsync();

                // Load related data for notifications
                await _context.Entry(request)
                    .Reference(r => r.BloodType)
                    .LoadAsync();
                await _context.Entry(request)
                    .Reference(r => r.Location)
                    .LoadAsync();

                // Send notifications to matching donors asynchronously (fire and forget)
                // This prevents blocking the request response
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var notificationCount = await _notificationService.NotifyMatchingDonorsAsync(request);
                        // Log success (you could also store this in a notification log table)
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request creation
                        // In production, you might want to use a proper logging service
                        Console.WriteLine($"Error sending notifications: {ex.Message}");
                    }
                });

                return RedirectToAction("RequestManagement");
            }

            // If model is invalid, redirect back with error
            return RedirectToAction("RequestManagement");
        }

        // POST: Admin/UpdateDonorRequestStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDonorRequestStatus(int requestId, string status)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var request = await _context.DonorRequests.FindAsync(requestId);
            if (request != null)
            {
                request.Status = status;
                if (status == "Completed")
                {
                    request.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("RequestManagement");
        }

        // GET: Admin/Settings
        public async Task<IActionResult> Settings()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.AdminName = user.FirstName + " " + user.LastName;
            ViewBag.UserRole = user.Role;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserCreatedAt = user.CreatedAt;

            return View(user);
        }

        // GET: Admin/AddDonor
        public async Task<IActionResult> AddDonor()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var viewModel = new AddDonorViewModel
            {
                BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync(),
                Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync()
            };

            ViewBag.AdminName = user.FirstName + " " + user.LastName;
            return View(viewModel);
        }

        // POST: Admin/AddDonor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDonor(AddDonorViewModel model)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            // Validate password only if adding new donor
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required for new donors.");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters.");
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
                    model.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
                    var user = await _userManager.GetUserAsync(User);
                    ViewBag.AdminName = user?.FirstName + " " + user?.LastName;
                    return View(model);
                }

                // Create user
                var newUser = new Users
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.Email,
                    EmailConfirmed = true,
                    Role = "Donor",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, model.Password ?? "");
                if (result.Succeeded)
                {
                    // Create donor profile
                    var donorProfile = new DonorProfile
                    {
                        DonorId = newUser.Id,
                        BloodTypeId = model.BloodTypeId,
                        LocationId = model.LocationId,
                        Gender = model.Gender,
                        DateOfBirth = model.DateOfBirth,
                        IsHealthyForDonation = model.IsHealthyForDonation,
                        IsAvailable = model.IsAvailable,
                        IsIdentityHidden = model.IsIdentityHidden,
                        LastDonationDate = model.LastDonationDate,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.DonorProfile.Add(donorProfile);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("DonorManagement");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            // If we get here, something went wrong
            model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            model.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
            var adminUser = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = adminUser?.FirstName + " " + adminUser?.LastName;
            return View(model);
        }

        // GET: Admin/EditDonor/{id}
        public async Task<IActionResult> EditDonor(int id)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var donorProfile = await _context.DonorProfile
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .Include(d => d.Location)
                .FirstOrDefaultAsync(d => d.DonorId == id);

            if (donorProfile == null)
            {
                return NotFound();
            }

            var viewModel = new AddDonorViewModel
            {
                FirstName = donorProfile.User.FirstName,
                LastName = donorProfile.User.LastName,
                Email = donorProfile.User.Email,
                PhoneNumber = donorProfile.User.PhoneNumber,
                BloodTypeId = donorProfile.BloodTypeId,
                LocationId = donorProfile.LocationId,
                Gender = donorProfile.Gender,
                DateOfBirth = donorProfile.DateOfBirth,
                IsHealthyForDonation = donorProfile.IsHealthyForDonation,
                IsAvailable = donorProfile.IsAvailable,
                IsIdentityHidden = donorProfile.IsIdentityHidden,
                LastDonationDate = donorProfile.LastDonationDate,
                BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync(),
                Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync()
            };

            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.FirstName + " " + user?.LastName;
            ViewBag.DonorId = id;
            return View("AddDonor", viewModel);
        }

        // POST: Admin/EditDonor/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDonor(int id, AddDonorViewModel model)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var donorProfile = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == id);

            if (donorProfile == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update user info
                donorProfile.User.FirstName = model.FirstName;
                donorProfile.User.LastName = model.LastName;
                donorProfile.User.PhoneNumber = model.PhoneNumber;

                // Check if email changed and if new email already exists
                if (donorProfile.User.Email != model.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != donorProfile.User.Id)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
                        model.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
                        var user = await _userManager.GetUserAsync(User);
                        ViewBag.AdminName = user?.FirstName + " " + user?.LastName;
                        ViewBag.DonorId = id;
                        return View(model);
                    }
                    donorProfile.User.Email = model.Email;
                    donorProfile.User.UserName = model.Email;
                }

                // Update password if provided and not empty
                if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length >= 6)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(donorProfile.User);
                    await _userManager.ResetPasswordAsync(donorProfile.User, token, model.Password);
                }

                // Update donor profile
                donorProfile.BloodTypeId = model.BloodTypeId;
                donorProfile.LocationId = model.LocationId;
                donorProfile.Gender = model.Gender;
                donorProfile.DateOfBirth = model.DateOfBirth;
                donorProfile.IsHealthyForDonation = model.IsHealthyForDonation;
                donorProfile.IsAvailable = model.IsAvailable;
                donorProfile.IsIdentityHidden = model.IsIdentityHidden;
                donorProfile.LastDonationDate = model.LastDonationDate;

                await _userManager.UpdateAsync(donorProfile.User);
                await _context.SaveChangesAsync();

                return RedirectToAction("DonorManagement");
            }

            // If we get here, something went wrong
            model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            model.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
            var adminUser = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = adminUser?.FirstName + " " + adminUser?.LastName;
            ViewBag.DonorId = id;
            return View("AddDonor", model);
        }

        // POST: Admin/DeleteDonor/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDonor(int id)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var donorProfile = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == id);

            if (donorProfile == null)
            {
                return NotFound();
            }

            // Delete donor profile first (due to foreign key constraint)
            _context.DonorProfile.Remove(donorProfile);
            await _context.SaveChangesAsync();

            // Then delete the user
            await _userManager.DeleteAsync(donorProfile.User);

            return RedirectToAction("DonorManagement");
        }
    }
}

