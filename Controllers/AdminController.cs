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
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            BloodDonationContext context, 
            UserManager<Users> userManager,
            NotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
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

            try
            {
                // Remove ModelState errors for navigation properties since we only bind IDs
                // These properties are null because we're only sending IDs from the form
                ModelState.Remove("BloodType");
                ModelState.Remove("Location");
                ModelState.Remove("RequestedByUser");

                // Validate required fields manually for better error messages
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.PatientName))
                {
                    errors.Add("Patient Name is required.");
                }
                else if (request.PatientName.Length > 200)
                {
                    errors.Add("Patient Name cannot exceed 200 characters.");
                }

                if (request.BloodTypeId <= 0)
                {
                    errors.Add("Blood Type is required. Please select a valid blood type.");
                }
                else
                {
                    // Verify blood type exists
                    var bloodTypeExists = await _context.BloodTypes.AnyAsync(bt => bt.BloodTypeId == request.BloodTypeId);
                    if (!bloodTypeExists)
                    {
                        errors.Add($"Selected Blood Type (ID: {request.BloodTypeId}) does not exist in the database.");
                    }
                }

                if (request.LocationId <= 0)
                {
                    errors.Add("Location is required. Please select a valid location.");
                }
                else
                {
                    // Verify location exists
                    var locationExists = await _context.Locations.AnyAsync(l => l.LocationId == request.LocationId);
                    if (!locationExists)
                    {
                        errors.Add($"Selected Location (ID: {request.LocationId}) does not exist in the database.");
                    }
                }

                if (string.IsNullOrWhiteSpace(request.UrgencyLevel))
                {
                    errors.Add("Urgency Level is required.");
                }
                else if (!new[] { "Critical", "High", "Normal", "Low" }.Contains(request.UrgencyLevel))
                {
                    errors.Add($"Invalid Urgency Level: '{request.UrgencyLevel}'. Must be one of: Critical, High, Normal, Low.");
                }

                if (string.IsNullOrWhiteSpace(request.ContactNumber))
                {
                    errors.Add("Contact Number is required.");
                }
                else if (request.ContactNumber.Length > 20)
                {
                    errors.Add("Contact Number cannot exceed 20 characters.");
                }

                if (string.IsNullOrWhiteSpace(request.RequesterEmail))
                {
                    errors.Add("Requester Email is required.");
                }
                else if (request.RequesterEmail.Length > 200)
                {
                    errors.Add("Requester Email cannot exceed 200 characters.");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(request.RequesterEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    errors.Add("Requester Email format is invalid.");
                }

                if (!string.IsNullOrWhiteSpace(request.HospitalName) && request.HospitalName.Length > 100)
                {
                    errors.Add("Hospital Name cannot exceed 100 characters.");
                }

                if (!string.IsNullOrWhiteSpace(request.AdditionalNotes) && request.AdditionalNotes.Length > 500)
                {
                    errors.Add("Additional Notes cannot exceed 500 characters.");
                }

                // If there are validation errors, return them
                if (errors.Any())
                {
                    var errorMessage = "Failed to create request:\n• " + string.Join("\n• ", errors);
                    TempData["ErrorMessage"] = errorMessage;
                    _logger.LogWarning("DonorRequest creation failed - Validation errors: {Errors}", string.Join("; ", errors));
                    return RedirectToAction("RequestManagement");
                }

                // Check for any remaining ModelState errors (excluding navigation properties we already removed)
                if (!ModelState.IsValid)
                {
                    var modelStateErrors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                        .ToList();

                    if (modelStateErrors.Any())
                    {
                        var errorMessage = "Failed to create request:\n• " + string.Join("\n• ", modelStateErrors);
                        TempData["ErrorMessage"] = errorMessage;
                        _logger.LogWarning("DonorRequest creation failed - ModelState errors: {Errors}", string.Join("; ", modelStateErrors));
                        return RedirectToAction("RequestManagement");
                    }
                }

                // All validations passed, proceed with creation
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    request.RequestedByUserId = user.Id;
                }
                else
                {
                    _logger.LogWarning("User not found when creating DonorRequest");
                }

                request.Status = "Pending";
                request.CreatedAt = DateTime.UtcNow;

                _context.DonorRequests.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DonorRequest created successfully - RequestId: {RequestId}, PatientName: {PatientName}, BloodTypeId: {BloodTypeId}, LocationId: {LocationId}",
                    request.RequestId, request.PatientName, request.BloodTypeId, request.LocationId);

                // Record tracked action
                await RecordActionAsync(
                    "Create Blood Donation Request",
                    ActionType.Create,
                    $"Created blood donation request for patient: {request.PatientName} (Blood Type ID: {request.BloodTypeId}, Location ID: {request.LocationId})",
                    targetEntityId: request.RequestId
                );

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
                        _logger.LogInformation("Notifications sent for RequestId {RequestId}: {Count} donors notified", request.RequestId, notificationCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending notifications for RequestId {RequestId}", request.RequestId);
                    }
                });

                TempData["SuccessMessage"] = $"Blood donation request created successfully! Request ID: {request.RequestId}. Notifications have been sent to matching donors.";
                return RedirectToAction("RequestManagement");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var errorDetails = new List<string> { "Database error occurred while creating the request." };
                
                // Check for foreign key constraint violations
                if (dbEx.InnerException != null)
                {
                    var innerMessage = dbEx.InnerException.Message;
                    if (innerMessage.Contains("foreign key") || innerMessage.Contains("FOREIGN KEY"))
                    {
                        errorDetails.Add("Foreign key constraint violation - The selected Blood Type or Location may not exist.");
                    }
                    else if (innerMessage.Contains("cannot be null") || innerMessage.Contains("NULL"))
                    {
                        errorDetails.Add("Required field is missing - Please ensure all required fields are filled.");
                    }
                    else
                    {
                        errorDetails.Add($"Database error: {innerMessage}");
                    }
                }

                _logger.LogError(dbEx, "Database error creating DonorRequest - BloodTypeId: {BloodTypeId}, LocationId: {LocationId}",
                    request.BloodTypeId, request.LocationId);

                TempData["ErrorMessage"] = string.Join("\n• ", errorDetails);
            return RedirectToAction("RequestManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating DonorRequest");
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}\n\nPlease check the logs for more details or contact support.";
                return RedirectToAction("RequestManagement");
            }
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
                TempData["SuccessMessage"] = $"Request #{requestId} status updated to '{status}' successfully.";

                // Record tracked action
                await RecordActionAsync(
                    "Update Request Status",
                    ActionType.Update,
                    $"Updated request #{requestId} status to {status}",
                    targetEntityId: requestId
                );
            }
            else
            {
                TempData["ErrorMessage"] = $"Request #{requestId} not found.";
            }

            return RedirectToAction("RequestManagement");
        }

        // GET: Admin/GetMatchingDonors/{requestId}
        [HttpGet]
        public async Task<IActionResult> GetMatchingDonors(int requestId)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            try
            {
                var request = await _context.DonorRequests
                    .Include(r => r.BloodType)
                    .Include(r => r.Location)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                {
                    return Json(new { success = false, message = "Request not found" });
                }

                // Find matching donors: same blood type, same location, available and healthy
                var matchingDonors = await _context.DonorProfile
                    .Include(d => d.User)
                    .Include(d => d.BloodType)
                    .Include(d => d.Location)
                    .Where(d => 
                        d.BloodTypeId == request.BloodTypeId &&
                        d.LocationId == request.LocationId &&
                        d.IsAvailable &&
                        d.IsHealthyForDonation)
                    .Select(d => new
                    {
                        DonorId = d.DonorId,
                        Name = d.IsIdentityHidden ? $"Donor #{d.DonorId}" : $"{d.User.FirstName} {d.User.LastName}",
                        Email = d.User.Email,
                        PhoneNumber = d.User.PhoneNumber ?? "N/A",
                        BloodType = d.BloodType.Type,
                        Location = d.Location.Districts,
                        LastDonationDate = d.LastDonationDate.HasValue ? d.LastDonationDate.Value.ToString("yyyy-MM-dd") : "Never",
                        IsAvailable = d.IsAvailable,
                        IsHealthy = d.IsHealthyForDonation
                    })
                    .ToListAsync();

                return Json(new { success = true, donors = matchingDonors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting matching donors for request {requestId}");
                return Json(new { success = false, message = "Error retrieving matching donors" });
            }
        }

        // POST: Admin/SendEmailsToSelectedDonors
        [HttpPost]
        [IgnoreAntiforgeryToken] // Using [Authorize] for security instead
        public async Task<IActionResult> SendEmailsToSelectedDonors([FromBody] SendEmailsRequest model)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            try
            {
                if (model == null || model.DonorIds == null || !model.DonorIds.Any())
                {
                    return Json(new { success = false, message = "No donors selected" });
                }

                var request = await _context.DonorRequests
                    .Include(r => r.BloodType)
                    .Include(r => r.Location)
                    .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

                if (request == null)
                {
                    return Json(new { success = false, message = "Request not found" });
                }

                // Get selected donors
                var selectedDonors = await _context.DonorProfile
                    .Include(d => d.User)
                    .Where(d => model.DonorIds.Contains(d.DonorId))
                    .ToListAsync();

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var donor in selectedDonors)
                {
                    if (donor.User != null && !string.IsNullOrEmpty(donor.User.Email))
                    {
                        var emailSent = await _notificationService.SendEmailNotificationToDonorAsync(donor.User, request);
                        if (emailSent)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            errors.Add($"Failed to send email to {donor.User.Email}");
                        }
                    }
                    else
                    {
                        failCount++;
                        errors.Add($"Donor {donor.DonorId} has no email address");
                    }
                }

                var message = $"Emails sent successfully to {successCount} donor(s).";
                if (failCount > 0)
                {
                    message += $" Failed to send to {failCount} donor(s).";
                }

                _logger.LogInformation($"Sent emails for request {model.RequestId}: {successCount} successful, {failCount} failed");

                // Record tracked action
                await RecordActionAsync(
                    "Send Emails to Donors",
                    ActionType.Update,
                    $"Sent emails to {successCount} donor(s) for request #{model.RequestId}. Failed: {failCount}",
                    targetEntityId: model.RequestId
                );

                return Json(new 
                { 
                    success = true, 
                    message = message,
                    successCount = successCount,
                    failCount = failCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending emails to selected donors for request {model?.RequestId ?? 0}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Admin/RecordDonorConfirmation
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RecordDonorConfirmation([FromBody] DonorConfirmationRequest model)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            try
            {
                if (model == null || model.RequestId <= 0 || model.DonorId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                // Check if confirmation already exists
                var existingConfirmation = await _context.DonorConfirmations
                    .FirstOrDefaultAsync(c => c.RequestId == model.RequestId && c.DonorId == model.DonorId);

                var isNewConfirmation = false;
                var shouldSendEmail = false;

                if (existingConfirmation != null)
                {
                    // Update existing confirmation
                    var wasNotConfirmed = existingConfirmation.Status != "Confirmed";
                    existingConfirmation.Status = model.Status ?? "Confirmed";
                    existingConfirmation.Message = model.Message;
                    existingConfirmation.ConfirmedAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(model.AdminNotes))
                    {
                        existingConfirmation.AdminNotes = model.AdminNotes;
                    }
                    // Send email if status changed to Confirmed
                    shouldSendEmail = wasNotConfirmed && existingConfirmation.Status == "Confirmed";
                }
                else
                {
                    // Create new confirmation
                    var confirmation = new DonorConfirmation
                    {
                        RequestId = model.RequestId,
                        DonorId = model.DonorId,
                        Status = model.Status ?? "Confirmed",
                        Message = model.Message,
                        ConfirmedAt = DateTime.UtcNow,
                        AdminNotes = model.AdminNotes
                    };

                    _context.DonorConfirmations.Add(confirmation);
                    isNewConfirmation = true;
                    shouldSendEmail = confirmation.Status == "Confirmed";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Donor confirmation recorded - RequestId: {model.RequestId}, DonorId: {model.DonorId}, Status: {model.Status}, ShouldSendEmail: {shouldSendEmail}");

                // Record tracked action
                var actionType = isNewConfirmation ? ActionType.Create : ActionType.Update;
                await RecordActionAsync(
                    isNewConfirmation ? "Record Donor Confirmation" : "Update Donor Confirmation",
                    actionType,
                    $"{(isNewConfirmation ? "Recorded" : "Updated")} donor confirmation - Request #{model.RequestId}, Donor #{model.DonorId}, Status: {model.Status ?? "Confirmed"}",
                    targetEntityId: model.RequestId,
                    targetUserId: model.DonorId
                );

                // Send email to requester if donor confirmed
                if (shouldSendEmail)
                {
                    var request = await _context.DonorRequests
                        .Include(r => r.BloodType)
                        .Include(r => r.Location)
                        .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

                    _logger.LogInformation($"Retrieved request {model.RequestId} - RequesterEmail: {(request?.RequesterEmail ?? "NULL")}");

                    if (request != null && !string.IsNullOrEmpty(request.RequesterEmail))
                    {
                        var donor = await _context.DonorProfile
                            .Include(d => d.User)
                            .Include(d => d.BloodType)
                            .Include(d => d.Location)
                            .FirstOrDefaultAsync(d => d.DonorId == model.DonorId);

                        if (donor != null)
                        {
                            // Send email asynchronously but don't block the response
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    _logger.LogInformation($"Attempting to send donor info email to {request.RequesterEmail} for request {model.RequestId}");
                                    var emailSent = await _notificationService.SendDonorInfoToRequesterAsync(donor, request);
                                    if (emailSent)
                                    {
                                        _logger.LogInformation($"Successfully sent donor info email to {request.RequesterEmail} for request {model.RequestId}");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Failed to send donor info email to {request.RequesterEmail} for request {model.RequestId} - SendDonorInfoToRequesterAsync returned false");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error sending donor info to requester {request.RequesterEmail} for request {model.RequestId}");
                                }
                            }).ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    _logger.LogError(task.Exception, $"Task failed while sending email to requester for request {model.RequestId}");
                                }
                            });
                        }
                        else
                        {
                            _logger.LogWarning($"Donor {model.DonorId} not found when trying to send email to requester for request {model.RequestId}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Request {model.RequestId} not found or has no requester email when trying to send confirmation email");
                    }
                }

                _logger.LogInformation($"Donor confirmation recorded - RequestId: {model.RequestId}, DonorId: {model.DonorId}, Status: {model.Status}");

                return Json(new { success = true, message = "Confirmation recorded successfully" + (shouldSendEmail ? ". Email sent to requester." : "") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording donor confirmation for request {model?.RequestId ?? 0}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Admin/GetDonorConfirmations/{requestId}
        [HttpGet]
        public async Task<IActionResult> GetDonorConfirmations(int requestId)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            try
            {
                // Load confirmations with related data first
                var confirmationsData = await _context.DonorConfirmations
                    .Include(c => c.Donor)
                        .ThenInclude(d => d.User)
                    .Where(c => c.RequestId == requestId)
                    .ToListAsync();

                // Then project to the desired format
                var confirmations = confirmationsData.Select(c => new
                {
                    confirmationId = c.ConfirmationId,
                    donorId = c.DonorId,
                    donorName = c.Donor?.IsIdentityHidden == true 
                        ? $"Donor #{c.DonorId}" 
                        : $"{c.Donor?.User?.FirstName ?? ""} {c.Donor?.User?.LastName ?? ""}".Trim(),
                    donorEmail = c.Donor?.User?.Email ?? "",
                    status = c.Status ?? "Pending",
                    message = c.Message,
                    confirmedAt = c.ConfirmedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    adminNotes = c.AdminNotes
                }).ToList();

                _logger.LogInformation($"Returning {confirmations.Count} confirmations for request {requestId}");
                return Json(new { success = true, confirmations = confirmations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting donor confirmations for request {requestId}");
                return Json(new { success = false, message = "Error retrieving confirmations" });
            }
        }

        // GET: Admin/GetNotificationCount
        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            try
            {
                // Count new confirmations from the last 24 hours
                var newConfirmationsCount = await _context.DonorConfirmations
                    .Where(c => c.ConfirmedAt >= DateTime.UtcNow.AddHours(-24) && c.Status == "Confirmed")
                    .CountAsync();

                // Get recent confirmations for dropdown - load data first, then project
                var recentConfirmationsData = await _context.DonorConfirmations
                    .Include(c => c.Donor)
                        .ThenInclude(d => d.User)
                    .Include(c => c.Request)
                        .ThenInclude(r => r.BloodType)
                    .Where(c => c.ConfirmedAt >= DateTime.UtcNow.AddHours(-24) && c.Status == "Confirmed")
                    .OrderByDescending(c => c.ConfirmedAt)
                    .Take(10)
                    .ToListAsync();

                // Project to desired format after loading
                var recentConfirmations = recentConfirmationsData.Select(c => new
                {
                    ConfirmationId = c.ConfirmationId,
                    RequestId = c.RequestId,
                    PatientName = c.Request?.PatientName ?? "Unknown",
                    BloodType = c.Request?.BloodType?.Type ?? "Unknown",
                    DonorName = c.Donor?.IsIdentityHidden == true 
                        ? $"Donor #{c.DonorId}" 
                        : $"{c.Donor?.User?.FirstName ?? ""} {c.Donor?.User?.LastName ?? ""}".Trim(),
                    ConfirmedAt = c.ConfirmedAt.ToString("yyyy-MM-dd HH:mm"),
                    TimeAgo = GetTimeAgo(c.ConfirmedAt)
                }).ToList();

                return Json(new 
                { 
                    success = true, 
                    count = newConfirmationsCount,
                    confirmations = recentConfirmations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification count");
                return Json(new { success = false, count = 0, confirmations = new List<object>() });
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour(s) ago";
            return $"{(int)timeSpan.TotalDays} day(s) ago";
        }

        /// <summary>
        /// Helper method to record tracked actions
        /// </summary>
        private async Task RecordActionAsync(string actionName, ActionType actionType, string? description = null, int? targetEntityId = null, int? targetUserId = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return;

                var trackedAction = new TrackedAction
                {
                    Name = actionName,
                    Description = description,
                    Type = actionType,
                    PerformedByUserId = user.Id,
                    PerformedAt = DateTime.UtcNow,
                    TargetEntityId = targetEntityId,
                    TargetUserId = targetUserId
                };

                _context.Actions.Add(trackedAction);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording tracked action: {actionName}");
                // Don't throw - tracking failures shouldn't break the main action
            }
        }

        // GET: Admin/TestEmailToRequester
        [HttpGet]
        public async Task<IActionResult> TestEmailToRequester(int requestId, string? testEmail = null)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            try
            {
                var request = await _context.DonorRequests
                    .Include(r => r.BloodType)
                    .Include(r => r.Location)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                {
                    return Json(new { success = false, message = $"Request {requestId} not found" });
                }

                var emailToSend = testEmail ?? request.RequesterEmail;
                if (string.IsNullOrEmpty(emailToSend))
                {
                    return Json(new { success = false, message = "No requester email found for this request" });
                }

                // Get a donor for testing (or use the first confirmed donor for this request)
                var donor = await _context.DonorProfile
                    .Include(d => d.User)
                    .Include(d => d.BloodType)
                    .Include(d => d.Location)
                    .FirstOrDefaultAsync(d => d.DonorId == 1); // Use first donor as test

                if (donor == null)
                {
                    return Json(new { success = false, message = "No donor found for testing" });
                }

                // Temporarily override the requester email for testing
                var originalEmail = request.RequesterEmail;
                request.RequesterEmail = emailToSend;

                var emailSent = await _notificationService.SendDonorInfoToRequesterAsync(donor, request);

                // Restore original email
                request.RequesterEmail = originalEmail;

                if (emailSent)
                {
                    return Json(new { success = true, message = $"Test email sent successfully to {emailToSend}" });
                }
                else
                {
                    return Json(new { success = false, message = "Email sending returned false. Check logs for details." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing email to requester for request {requestId}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
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

        // POST: Admin/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Settings");
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                TempData["ErrorMessage"] = "Current password is required.";
                return RedirectToAction("Settings");
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "New password must be at least 6 characters long.";
                return RedirectToAction("Settings");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation password do not match.";
                return RedirectToAction("Settings");
            }

            // Verify current password
            var passwordValid = await _userManager.CheckPasswordAsync(user, CurrentPassword);
            if (!passwordValid)
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Settings");
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";

                // Record tracked action
                await RecordActionAsync(
                    "Change Password",
                    ActionType.Update,
                    "Admin changed their account password",
                    targetUserId: user.Id
                );

                _logger.LogInformation($"Admin {user.Id} changed their password successfully");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Failed to change password: {errors}";
                _logger.LogWarning($"Failed to change password for admin {user.Id}: {errors}");
            }

            return RedirectToAction("Settings");
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

                TempData["SuccessMessage"] = $"Donor '{model.FirstName} {model.LastName}' added successfully!";

                // Record tracked action
                await RecordActionAsync(
                    "Add Donor",
                    ActionType.Create,
                    $"Added new donor: {model.FirstName} {model.LastName} ({model.Email})",
                    targetEntityId: donorProfile.DonorId,
                    targetUserId: newUser.Id
                );

                return RedirectToAction("DonorManagement");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    TempData["ErrorMessage"] = "Failed to create donor. " + string.Join(" ", result.Errors.Select(e => e.Description));
                }
            }

            // If we get here, something went wrong
            model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            model.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
            var adminUser = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = adminUser?.FirstName + " " + adminUser?.LastName;
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
            }
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
                TempData["ErrorMessage"] = $"Donor with ID {id} not found.";
                return RedirectToAction("DonorManagement");
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
                TempData["ErrorMessage"] = $"Donor with ID {id} not found.";
                return RedirectToAction("DonorManagement");
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

                TempData["SuccessMessage"] = $"Donor '{model.FirstName} {model.LastName}' updated successfully!";

                // Record tracked action
                await RecordActionAsync(
                    "Edit Donor",
                    ActionType.Update,
                    $"Updated donor profile: {model.FirstName} {model.LastName} (Donor ID: {id})",
                    targetEntityId: id,
                    targetUserId: donorProfile.DonorId
                );

                return RedirectToAction("DonorManagement");
            }

            // If we get here, something went wrong
            model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            model.Locations = await _context.Locations.OrderBy(l => l.Districts).ToListAsync();
            var adminUser = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = adminUser?.FirstName + " " + adminUser?.LastName;
            ViewBag.DonorId = id;
            TempData["ErrorMessage"] = "Failed to update donor. Please check the form for errors.";
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
                TempData["ErrorMessage"] = $"Donor with ID {id} not found.";
                return RedirectToAction("DonorManagement");
            }

            var donorName = $"{donorProfile.User.FirstName} {donorProfile.User.LastName}";

            try
            {
            // Delete donor profile first (due to foreign key constraint)
            _context.DonorProfile.Remove(donorProfile);
            await _context.SaveChangesAsync();

            // Then delete the user
                var deleteResult = await _userManager.DeleteAsync(donorProfile.User);
                
                if (deleteResult.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Donor '{donorName}' deleted successfully!";

                    // Record tracked action
                    await RecordActionAsync(
                        "Delete Donor",
                        ActionType.Delete,
                        $"Deleted donor: {donorName} (Donor ID: {id})",
                        targetEntityId: id,
                        targetUserId: id
                    );
                }
                else
                {
                    TempData["WarningMessage"] = $"Donor profile deleted, but there was an issue deleting the user account: {string.Join(" ", deleteResult.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete donor '{donorName}': {ex.Message}";
            }

            return RedirectToAction("DonorManagement");
        }
    }

    // Request model for sending emails
    public class SendEmailsRequest
    {
        public int RequestId { get; set; }
        public List<int> DonorIds { get; set; } = new List<int>();
    }

    // Request model for donor confirmation
    public class DonorConfirmationRequest
    {
        public int RequestId { get; set; }
        public int DonorId { get; set; }
        public string? Status { get; set; } // Confirmed, Declined, Pending
        public string? Message { get; set; }
        public string? AdminNotes { get; set; }
    }
}

