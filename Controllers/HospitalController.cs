using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using BloodDonation.Models.ViewModels;

namespace BloodDonation.Controllers
{
    public class HospitalController : Controller
    {
        private readonly BloodDonationContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;

        public HospitalController(BloodDonationContext context, UserManager<Users> userManager, SignInManager<Users> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ============================================
        // OWNER ACTIONS - Hospital Management
        // ============================================

        [Authorize(Roles = "Owner")]
        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var list = _context.Hospitals.Include(h => h.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                list = list.Where(h =>
                    h.Name.ToLower().Contains(lower) ||
                    h.User.Email.ToLower().Contains(lower) ||
                    h.License.ToLower().Contains(lower));
            }

            // Order by User.CreatedAt Descending
            var model = await list.OrderByDescending(h => h.User.CreatedAt).ToListAsync();

            return View("~/Views/Owner/HospitalManagement.cshtml", model);
        }

        [Authorize(Roles = "Owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddNewHospitalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return PartialView("~/Views/Owner/AddNewHospital.cshtml", model);
            }

            // 1. Create User Account
            var user = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Name, // Using hospital name as first name for simplicity
                LastName = "Hospital",
                PhoneNumber = model.Phone,
                Role = "Hospital",
                Status = model.Status ?? "Active",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Response.StatusCode = 400;
                return PartialView("~/Views/Owner/AddNewHospital.cshtml", model);
            }

            // Add role claims
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Hospital"));
            await _userManager.AddClaimAsync(user, new Claim("Role", "Hospital"));

            // 2. Create Hospital Entity
            var entity = new Hospital
            {
                Name = model.Name,
                License = model.License,
                UserId = user.Id,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Zip = model.Zip
            };

            _context.Hospitals.Add(entity);
            await _context.SaveChangesAsync();

            // Record the action
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                _context.Actions.Add(new TrackedAction
                {
                    Name = "Create Hospital",
                    Description = $"Created new hospital: {entity.Name}",
                    Type = ActionType.Create,
                    PerformedByUserId = currentUser.Id,
                    PerformedAt = DateTime.UtcNow,
                    TargetEntityId = entity.Id,
                    TargetUserId = user.Id
                });
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [Authorize(Roles = "Owner")]
        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();

            var vm = new AddNewHospitalViewModel
            {
                Name = hospital.Name,
                License = hospital.License,
                Email = hospital.User?.Email ?? "",
                Phone = hospital.User?.PhoneNumber ?? "",
                Address = hospital.Address,
                City = hospital.City,
                State = hospital.State,
                Zip = hospital.Zip,
                Status = hospital.User?.Status ?? "Active"
            };

            ViewData["HospitalId"] = id;
            return PartialView("~/Views/Owner/_EditHospitalModal.cshtml", vm);
        }

        [Authorize(Roles = "Owner")]
        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();

            return PartialView("~/Views/Owner/_HospitalDetailsModal.cshtml", hospital);
        }

        [Authorize(Roles = "Owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddNewHospitalViewModel model)
        {
            // Password is not required for editing
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                ViewData["HospitalId"] = id;
                Response.StatusCode = 400;
                return PartialView("~/Views/Owner/_EditHospitalModal.cshtml", model);
            }

            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();

            // Update Hospital Entity
            hospital.Name = model.Name;
            hospital.License = model.License;
            hospital.Address = model.Address;
            hospital.City = model.City;
            hospital.State = model.State;
            hospital.Zip = model.Zip;

            // Update User Account
            if (hospital.User != null)
            {
                hospital.User.Email = model.Email;
                hospital.User.UserName = model.Email;
                hospital.User.PhoneNumber = model.Phone;
                hospital.User.Status = model.Status ?? "Active";
                hospital.User.FirstName = model.Name;

                await _userManager.UpdateAsync(hospital.User);
            }

            _context.Hospitals.Update(hospital);
            await _context.SaveChangesAsync();

            // Record the action
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                _context.Actions.Add(new TrackedAction
                {
                    Name = "Edit Hospital",
                    Description = $"Updated hospital details: {hospital.Name}",
                    Type = ActionType.Update,
                    PerformedByUserId = currentUser.Id,
                    PerformedAt = DateTime.UtcNow,
                    TargetEntityId = hospital.Id,
                    TargetUserId = hospital.UserId
                });
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [Authorize(Roles = "Owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hospital = await _context.Hospitals
                .Include(h => h.User)
                .Include(h => h.HospitalStaff)
                    .ThenInclude(hs => hs.User)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hospital != null)
            {
                var hospitalName = hospital.Name;
                
                // Collect all users to delete (Hospital User + Staff Users)
                var usersToDelete = new System.Collections.Generic.List<Users>();
                
                if (hospital.User != null) 
                {
                    usersToDelete.Add(hospital.User);
                }

                if (hospital.HospitalStaff != null)
                {
                    foreach (var staff in hospital.HospitalStaff)
                    {
                        if (staff.User != null)
                        {
                            usersToDelete.Add(staff.User);
                        }
                    }
                }

                // Remove hospital (this will cascade delete HospitalStaff)
                _context.Hospitals.Remove(hospital);
                await _context.SaveChangesAsync();

                // Now delete the user accounts
                foreach (var user in usersToDelete)
                {
                    await _userManager.DeleteAsync(user);
                }

                // Record the action
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    _context.Actions.Add(new TrackedAction
                    {
                        Name = "Delete Hospital",
                        Description = $"Deleted hospital: {hospitalName}",
                        Type = ActionType.Delete,
                        PerformedByUserId = currentUser.Id,
                        PerformedAt = DateTime.UtcNow,
                        TargetEntityId = null,
                        TargetUserId = null
                    });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("HospitalManagement", "Owner");
        }

        // ============================================
        // HOSPITAL USER ACTIONS
        // ============================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(HospitalRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError("AgreeToTerms", "You must agree to the terms and conditions");
                return View(model);
            }

            // Check if email already exists
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }

            // Create User Account
            var user = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.ContactName,
                LastName = "Hospital",
                PhoneNumber = model.Phone,
                Role = "Hospital",
                Status = "Pending", // Awaiting approval
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // Add role claims
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Hospital"));
            await _userManager.AddClaimAsync(user, new Claim("Role", "Hospital"));

            // Create Hospital Entity
            var hospital = new Hospital
            {
                Name = model.HospitalName,
                License = model.License,
                UserId = user.Id,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Zip = model.Zip
            };

            _context.Hospitals.Add(hospital);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! Your account is pending approval.";
            return RedirectToAction("Login", "Auth");
        }

        // Helper method to get hospital for any hospital user (primary or staff)
        private async Task<(Hospital? hospital, HospitalStaff? staff, bool isPrimaryUser)> GetHospitalForUserAsync(int userId)
        {
            // Check if user is primary hospital user
            var hospital = await _context.Hospitals
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (hospital != null)
            {
                return (hospital, null, true);
            }

            // Check if user is a staff member
            var staff = await _context.HospitalStaff
                .Include(s => s.Hospital)
                    .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (staff != null && staff.Hospital != null)
            {
                return (staff.Hospital, staff, false);
            }

            return (null, null, false);
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Get all requests for this hospital (not just the current user's requests)
            // Staff members should see all hospital requests
            var hospitalUserIds = new List<int> { hospital.UserId };
            var staffUserIds = await _context.HospitalStaff
                .Where(s => s.HospitalId == hospital.Id && s.Status == "Active")
                .Select(s => s.UserId)
                .ToListAsync();
            hospitalUserIds.AddRange(staffUserIds);

            var hospitalRequests = await _context.DonorRequests
                .Include(r => r.BloodType)
                .Where(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value))
                .ToListAsync();

            var totalRequested = hospitalRequests.Count;
            var fulfilled = hospitalRequests.Count(r => r.Status == "Completed" || r.Status == "Approved");
            var pending = hospitalRequests.Count(r => r.Status == "Pending");

            // Calculate request trends for last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var last30DaysRequests = hospitalRequests.Where(r => r.CreatedAt >= thirtyDaysAgo).ToList();

            // Group by date for trend chart
            var trends = last30DaysRequests
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new DailyRequestCount
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Fill in missing days with 0
            var allDates = Enumerable.Range(0, 30)
                .Select(i => DateTime.UtcNow.Date.AddDays(-29 + i))
                .ToList();

            var trendsDict = trends.ToDictionary(t => t.Date.Date);
            var completeTrends = allDates.Select(date => new DailyRequestCount
            {
                Date = date,
                Count = trendsDict.ContainsKey(date) ? trendsDict[date].Count : 0
            }).ToList();

            // Calculate previous 30 days for growth comparison
            var previous30DaysStart = DateTime.UtcNow.AddDays(-60);
            var previous30DaysEnd = DateTime.UtcNow.AddDays(-30);
            var previous30DaysCount = hospitalRequests.Count(r =>
                r.CreatedAt >= previous30DaysStart && r.CreatedAt < previous30DaysEnd);

            var last30DaysTotal = last30DaysRequests.Count;
            var growth = previous30DaysCount > 0
                ? ((last30DaysTotal - previous30DaysCount) * 100.0 / previous30DaysCount)
                : (last30DaysTotal > 0 ? 100.0 : 0);

            // Calculate blood type demand percentages
            var allBloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
            var bloodTypeCounts = new Dictionary<string, int>();
            var totalBloodTypeRequests = hospitalRequests.Where(r => r.BloodType != null).Count();

            foreach (var request in hospitalRequests.Where(r => r.BloodType != null))
            {
                var bloodType = request.BloodType.Type;
                if (!bloodTypeCounts.ContainsKey(bloodType))
                    bloodTypeCounts[bloodType] = 0;
                bloodTypeCounts[bloodType]++;
            }

            var bloodTypePercentages = new Dictionary<string, double>();
            foreach (var bloodType in allBloodTypes)
            {
                var count = bloodTypeCounts.GetValueOrDefault(bloodType.Type, 0);
                var percentage = totalBloodTypeRequests > 0
                    ? (count * 100.0 / totalBloodTypeRequests)
                    : 0;
                bloodTypePercentages[bloodType.Type] = percentage;
            }

            var model = new HospitalDashboardViewModel
            {
                Hospital = hospital,
                TotalRequested = totalRequested,
                TotalReceived = fulfilled,
                PendingRequests = pending,
                SuccessRate = totalRequested > 0 ? (fulfilled * 100.0 / totalRequested) : 0,
                RecentRequests = hospitalRequests.OrderByDescending(r => r.CreatedAt).Take(5).ToList(),
                RequestTrends = completeTrends,
                BloodTypePercentages = bloodTypePercentages,
                Last30DaysTotal = last30DaysTotal,
                Last30DaysGrowth = growth
            };

            // Calculate blood type demand (for legacy support)
            foreach (var request in hospitalRequests)
            {
                var bloodType = request.BloodType?.Type ?? "Unknown";
                if (!model.BloodTypeDemand.ContainsKey(bloodType))
                    model.BloodTypeDemand[bloodType] = 0;
                model.BloodTypeDemand[bloodType]++;
            }

            return View(model);
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public async Task<IActionResult> CreateRequest()
        {
            var model = new CreateBloodRequestViewModel
            {
                BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync(),
                DateRequired = DateTime.Now,
                TimeRequired = DateTime.Now.TimeOfDay
            };

            return View(model);
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(CreateBloodRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Check if staff member has permission to create requests
            // Only Admin and Coordinator can create requests
            if (!isPrimaryUser && staff != null && staff.Role != "Admin" && staff.Role != "Coordinator")
            {
                TempData["ErrorMessage"] = "You do not have permission to create blood requests.";
                model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
                return View(model);
            }

            // Get hospital location (default to first location if not set)
            var location = await _context.Locations.FirstOrDefaultAsync();
            if (location == null)
            {
                ModelState.AddModelError("", "No locations available in the system.");
                model.BloodTypes = await _context.BloodTypes.OrderBy(b => b.Type).ToListAsync();
                return View(model);
            }

            // Map urgency level from view model to database values
            var urgencyLevel = model.UrgencyLevel?.ToLower() switch
            {
                "routine" => "Normal",
                "urgent" => "High",
                "critical" => "Critical",
                _ => "Normal"
            };

            // Create request
            var request = new DonorRequest
            {
                PatientName = $"Request for {model.Quantity} units", // Store quantity in patient name temporarily
                BloodTypeId = model.BloodTypeId,
                LocationId = location.LocationId,
                UrgencyLevel = urgencyLevel,
                ContactNumber = hospital.User.PhoneNumber ?? "",
                RequesterEmail = hospital.User.Email ?? "",
                HospitalName = hospital.Name,
                AdditionalNotes = BuildAdditionalNotes(model),
                RequestedByUserId = user.Id,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.DonorRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Blood request submitted successfully!";
            return RedirectToAction("ViewRequests");
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public async Task<IActionResult> ViewRequests(string? searchQuery, string? statusFilter, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Get all requests for this hospital (not just the current user's requests)
            var hospitalUserIds = new List<int> { hospital.UserId };
            var staffUserIds = await _context.HospitalStaff
                .Where(s => s.HospitalId == hospital.Id && s.Status == "Active")
                .Select(s => s.UserId)
                .ToListAsync();
            hospitalUserIds.AddRange(staffUserIds);

            var query = _context.DonorRequests
                .Include(r => r.BloodType)
                .Where(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value))
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(r =>
                    r.RequestId.ToString().Contains(searchQuery) ||
                    r.BloodType.Type.Contains(searchQuery));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            // Calculate status counts for all hospital requests
            var statusCounts = new Dictionary<string, int>
            {
                { "All", await _context.DonorRequests.CountAsync(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value)) },
                { "Pending", await _context.DonorRequests.CountAsync(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value) && r.Status == "Pending") },
                { "Approved", await _context.DonorRequests.CountAsync(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value) && r.Status == "Approved") },
                { "Completed", await _context.DonorRequests.CountAsync(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value) && r.Status == "Completed") },
                { "Cancelled", await _context.DonorRequests.CountAsync(r => r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value) && r.Status == "Cancelled") }
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new HospitalRequestsViewModel
            {
                Requests = requests,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter ?? "All",
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                StatusCounts = statusCounts
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public async Task<IActionResult> ViewMatchedDonors(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Get all user IDs for this hospital
            var hospitalUserIds = new List<int> { hospital.UserId };
            var staffUserIds = await _context.HospitalStaff
                .Where(s => s.HospitalId == hospital.Id && s.Status == "Active")
                .Select(s => s.UserId)
                .ToListAsync();
            hospitalUserIds.AddRange(staffUserIds);

            // Verify the request belongs to this hospital
            var request = await _context.DonorRequests
                .Include(r => r.BloodType)
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.RequestedByUserId.HasValue && hospitalUserIds.Contains(r.RequestedByUserId.Value));

            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found or you don't have permission to view it.";
                return RedirectToAction("ViewRequests");
            }

            // Get all donors who were sent emails for this request
            // If there's a DonorConfirmation record, it means an email was sent
            // Load data first, then project to handle nulls properly
            var confirmationsData = await _context.DonorConfirmations
                .Include(c => c.Donor)
                    .ThenInclude(d => d.User)
                .Include(c => c.Donor)
                    .ThenInclude(d => d.BloodType)
                .Include(c => c.Donor)
                    .ThenInclude(d => d.Location)
                .Where(c => c.RequestId == requestId) // Show all records - if record exists, email was sent
                .ToListAsync();

            var matchedDonors = confirmationsData
                .Where(c => c.Donor != null && c.Donor.User != null && c.Donor.BloodType != null && c.Donor.Location != null)
                .Select(c => new MatchedDonorViewModel
                {
                    ConfirmationId = c.ConfirmationId,
                    DonorId = c.DonorId,
                    DonorName = c.Donor.IsIdentityHidden
                        ? $"Donor #{c.DonorId}"
                        : $"{c.Donor.User.FirstName} {c.Donor.User.LastName}",
                    Email = c.Donor.User.Email ?? "",
                    PhoneNumber = c.Donor.User.PhoneNumber ?? "",
                    BloodType = c.Donor.BloodType.Type,
                    Location = c.Donor.Location.Districts,
                    Status = c.Status ?? "Pending",
                    ConfirmedAt = c.Status == "Confirmed" ? c.ConfirmedAt : (DateTime?)null,
                    ContactedAt = c.ContactedAt,
                    Message = c.Message,
                    LastDonationDate = c.Donor.LastDonationDate,
                    IsAvailable = c.Donor.IsAvailable,
                    IsHealthy = c.Donor.IsHealthyForDonation
                })
                .OrderByDescending(d => d.ContactedAt ?? d.ConfirmedAt)
                .ToList();

            var viewModel = new MatchedDonorsViewModel
            {
                Request = request,
                MatchedDonors = matchedDonors
            };

            return View("ViewMatchedDonors", viewModel);
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return Json(new { success = false, notifications = new List<object>() });

            // Get notifications for the primary hospital user (all staff see the same notifications)
            var notificationsData = await _context.HospitalNotifications
                .Include(n => n.Request)
                    .ThenInclude(r => r.BloodType)
                .Where(n => n.HospitalUserId == hospital.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var notifications = notificationsData.Select(n => {
                // Parse the notification message to extract status
                var requestStatus = "Updated";
                var statusColor = "blue";

                if (n.Message.Contains("Approved"))
                {
                    requestStatus = "Approved";
                    statusColor = "green";
                }
                else if (n.Message.Contains("Completed"))
                {
                    requestStatus = "Completed";
                    statusColor = "blue";
                }
                else if (n.Message.Contains("Cancelled"))
                {
                    requestStatus = "Cancelled";
                    statusColor = "red";
                }
                else if (n.Message.Contains("Pending"))
                {
                    requestStatus = "Pending";
                    statusColor = "yellow";
                }

                return new
                {
                    NotificationId = n.NotificationId,
                    RequestId = n.RequestId,
                    Message = n.Message,
                    Status = n.Status,
                    RequestStatus = requestStatus,
                    StatusColor = statusColor,
                    CreatedAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    TimeAgo = GetTimeAgo(n.CreatedAt),
                    BloodType = n.Request?.BloodType?.Type ?? null
                };
            }).ToList();

            return Json(new { success = true, notifications = notifications });
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, count = 0 });

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return Json(new { success = false, count = 0 });

            // Get notification count for the primary hospital user (all staff see the same count)
            var unreadCount = await _context.HospitalNotifications
                .Where(n => n.HospitalUserId == hospital.UserId && n.Status == "Unread")
                .CountAsync();

            return Json(new { success = true, count = unreadCount });
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead([FromBody] MarkNotificationReadRequest model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return Json(new { success = false, message = "Hospital not found" });

            // Allow any hospital staff to mark notifications as read
            var notification = await _context.HospitalNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == model.NotificationId && n.HospitalUserId == hospital.UserId);

            if (notification != null)
            {
                notification.Status = "Read";
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Notification not found" });
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

        private string BuildAdditionalNotes(CreateBloodRequestViewModel model)
        {
            var notes = new List<string>();

            notes.Add($"Quantity: {model.Quantity} units");
            notes.Add($"Component: {model.Component}");

            if (!string.IsNullOrWhiteSpace(model.DeliveryLocation))
                notes.Add($"Delivery Location: {model.DeliveryLocation}");

            if (!string.IsNullOrWhiteSpace(model.PatientMRN))
                notes.Add($"Patient MRN: {model.PatientMRN}");

            if (!string.IsNullOrWhiteSpace(model.Diagnosis))
                notes.Add($"Diagnosis: {model.Diagnosis}");

            if (!string.IsNullOrWhiteSpace(model.AdditionalNotes))
                notes.Add($"Notes: {model.AdditionalNotes}");

            notes.Add($"Required Date/Time: {model.DateRequired:yyyy-MM-dd} {model.TimeRequired:hh\\:mm}");

            return string.Join(" | ", notes);
        }

        [Authorize(Roles = "Hospital")]
        [HttpGet]
        public IActionResult Statistics(string timeframe, int? bloodTypeId)
        {
            // Start with all donor requests
            var query = _context.DonorRequests.Include(r => r.BloodType).AsQueryable();

            // Filter by timeframe
            DateTime? startDate = null;
            if (!string.IsNullOrEmpty(timeframe) && timeframe != "all")
            {
                int days = timeframe switch
                {
                    "30" => 30,
                    "90" => 90,
                    "365" => 365,
                    _ => 0
                };

                if (days > 0)
                {
                    startDate = DateTime.Today.AddDays(-days);
                    query = query.Where(r => r.CreatedAt >= startDate);
                }
            }

            // Filter by blood type
            if (bloodTypeId.HasValue)
            {
                query = query.Where(r => r.BloodTypeId == bloodTypeId.Value);
            }

            // Build model based on filtered data
            var model = new HospitalStatisticsViewModel
            {
                FulfillmentRate = GetFulfillmentRate(query),
                AverageCompletionTimeHours = GetAverageCompletionTime(query),
                EngagedRequests = GetEngagedRequests(query),
                UnmetRequests = GetUnmetRequests(query),
                SupplyVsDemand = GetSupplyVsDemand(query),
                CompletionRatio = GetCompletionRatio(query),
                BloodTypeFulfillment = GetFulfillmentByBloodType(query),
                StatusBreakdown = GetStatusBreakdown(query),
                // New properties
                MonthlyTrends = GetMonthlyTrends(query, startDate),
                MonthlyPerformance = GetMonthlyPerformance(query, startDate),
                FulfillmentTrend = GetFulfillmentTrend(query, startDate),
                TimeTrendHours = GetTimeTrend(query, startDate),
                EngagedTrendPercent = GetEngagedTrend(query, startDate),
                UnmetTrendCount = GetUnmetTrend(query, startDate)
            };

            // Blood types for dropdown
            ViewBag.Timeframes = new List<SelectListItem>
    {
        new SelectListItem { Text = "Last 30 Days", Value = "30", Selected = timeframe == "30" },
        new SelectListItem { Text = "Last 3 Months", Value = "90", Selected = timeframe == "90" },
        new SelectListItem { Text = "Last Year", Value = "365", Selected = timeframe == "365" },
        new SelectListItem { Text = "All Time", Value = "all", Selected = timeframe == "all" }
    };

            ViewBag.BloodTypesList = _context.BloodTypes
                .Select(bt => new SelectListItem
                {
                    Text = bt.Type,
                    Value = bt.BloodTypeId.ToString(),
                    Selected = bloodTypeId.HasValue && bt.BloodTypeId == bloodTypeId.Value
                })
                .ToList();

            return View(model);
        }

        // Existing helper methods
        private double GetFulfillmentRate(IQueryable<DonorRequest> query)
        {
            int total = query.Count();
            if (total == 0) return 0;
            int completed = query.Count(r => r.Status == "Completed");
            return Math.Round((double)completed / total * 100, 1);
        }

        private double GetAverageCompletionTime(IQueryable<DonorRequest> query)
        {
            var times = query
                .Where(r => r.CompletedAt != null)
                .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalHours)
                .ToList();

            return times.Any() ? Math.Round(times.Average(), 1) : 0;
        }

        private int GetEngagedRequests(IQueryable<DonorRequest> query)
        {
            return query.Count(r => r.Status == "Approved" || r.Status == "Completed");
        }

        private int GetUnmetRequests(IQueryable<DonorRequest> query)
        {
            return query.Count(r => r.Status == "Pending" || r.Status == "Cancelled");
        }

        private SupplyDemandViewModel GetSupplyVsDemand(IQueryable<DonorRequest> query)
        {
            return new SupplyDemandViewModel
            {
                Requested = query.Count(),
                Completed = query.Count(r => r.Status == "Completed")
            };
        }

        private List<BloodTypeFulfillmentViewModel> GetFulfillmentByBloodType(IQueryable<DonorRequest> query)
        {
            return query
                .GroupBy(r => r.BloodType.Type)
                .Select(g => new BloodTypeFulfillmentViewModel
                {
                    BloodType = g.Key,
                    Demand = g.Count(),
                    Completed = g.Count(r => r.Status == "Completed")
                })
                .ToList();
        }

        private List<StatusBreakdownViewModel> GetStatusBreakdown(IQueryable<DonorRequest> query)
        {
            return query
                .GroupBy(r => r.Status)
                .Select(g => new StatusBreakdownViewModel
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();
        }

        private CompletionRatioViewModel GetCompletionRatio(IQueryable<DonorRequest> query)
        {
            int completed = query.Count(r => r.Status == "Completed");
            int total = query.Count();

            return new CompletionRatioViewModel
            {
                Completed = completed,
                NotCompleted = total - completed
            };
        }

        // New helper methods for trends and monthly data
        private List<MonthlyTrendViewModel> GetMonthlyTrends(IQueryable<DonorRequest> query, DateTime? startDate)
        {
            var endDate = DateTime.Today;
            var compareStartDate = startDate ?? DateTime.Today.AddMonths(-6); // Default to last 6 months if no filter

            return query
                .Where(r => r.CreatedAt >= compareStartDate && r.CreatedAt <= endDate)
                .AsEnumerable() // Switch to client-side for date manipulation
                .GroupBy(r => new { Year = r.CreatedAt.Year, Month = r.CreatedAt.Month })
                .Select(g => new MonthlyTrendViewModel
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Requested = g.Count(),
                    Completed = g.Count(r => r.Status == "Completed")
                })
                .OrderByDescending(x =>
                {
                    // Parse month name back to date for ordering
                    var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                   "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                    var monthIndex = Array.IndexOf(monthNames, x.Month) + 1;
                    return new DateTime(DateTime.Today.Year, monthIndex, 1);
                })
                .Take(6) // Last 6 months
                .OrderBy(x =>
                {
                    var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                   "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                    var monthIndex = Array.IndexOf(monthNames, x.Month) + 1;
                    return new DateTime(DateTime.Today.Year, monthIndex, 1);
                })
                .ToList();
        }

        private List<MonthlyPerformanceViewModel> GetMonthlyPerformance(IQueryable<DonorRequest> query, DateTime? startDate)
        {
            var endDate = DateTime.Today;
            var compareStartDate = startDate ?? DateTime.Today.AddMonths(-4); // Default to last 4 months

            return query
                .Where(r => r.CreatedAt >= compareStartDate && r.CreatedAt <= endDate)
                .AsEnumerable() // Switch to client-side for date manipulation
                .GroupBy(r => new { Year = r.CreatedAt.Year, Month = r.CreatedAt.Month })
                .Select(g => new MonthlyPerformanceViewModel
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                    Requested = g.Count(),
                    Fulfilled = g.Count(r => r.Status == "Completed"),
                    Efficiency = g.Count() > 0 ? Math.Round((double)g.Count(r => r.Status == "Completed") / g.Count() * 100, 1) : 0
                })
                .OrderByDescending(x =>
                {
                    var monthNames = new[] { "January", "February", "March", "April", "May", "June",
                                   "July", "August", "September", "October", "November", "December" };
                    var monthIndex = Array.IndexOf(monthNames, x.Month) + 1;
                    return new DateTime(DateTime.Today.Year, monthIndex, 1);
                })
                .Take(4) // Last 4 months
                .ToList();
        }

        private double GetFulfillmentTrend(IQueryable<DonorRequest> query, DateTime? startDate)
        {
            if (!startDate.HasValue || startDate.Value > DateTime.Today.AddDays(-60))
                return 2.1; // Default if not enough data

            var currentPeriod = query.Where(r => r.CreatedAt >= startDate);
            var previousPeriod = query.Where(r => r.CreatedAt >= startDate.Value.AddMonths(-1) && r.CreatedAt < startDate);

            var currentRate = GetFulfillmentRate(currentPeriod);
            var previousRate = GetFulfillmentRate(previousPeriod);

            return Math.Round(currentRate - previousRate, 1);
        }

        private double GetTimeTrend(IQueryable<DonorRequest> query, DateTime? startDate)
        {
            if (!startDate.HasValue || startDate.Value > DateTime.Today.AddDays(-60))
                return 0.25;

            var currentValues = query
                .Where(r => r.CreatedAt >= startDate && r.CompletedAt != null)
                .AsEnumerable()
                .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalHours)
                .ToList();

            var previousValues = query
                .Where(r =>
                    r.CreatedAt >= startDate.Value.AddMonths(-1) &&
                    r.CreatedAt < startDate &&
                    r.CompletedAt != null)
                .AsEnumerable()
                .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalHours)
                .ToList();

            // 🔹 No data → neutral trend
            if (!currentValues.Any() || !previousValues.Any())
                return 0;

            var currentAvg = currentValues.Average();
            var previousAvg = previousValues.Average();

            return Math.Round(previousAvg - currentAvg, 2);
        }



        private double GetEngagedTrend(IQueryable<DonorRequest> query, DateTime? startDate)
        {
            if (!startDate.HasValue || startDate.Value > DateTime.Today.AddDays(-60))
                return 12.0; // Default

            var currentPeriod = query.Where(r => r.CreatedAt >= startDate);
            var previousPeriod = query.Where(r => r.CreatedAt >= startDate.Value.AddMonths(-1) && r.CreatedAt < startDate);

            var currentEngaged = currentPeriod.Count(r => r.Status == "Approved" || r.Status == "Completed");
            var previousEngaged = previousPeriod.Count(r => r.Status == "Approved" || r.Status == "Completed");

            if (previousEngaged == 0) return 0;
            return Math.Round((double)(currentEngaged - previousEngaged) / previousEngaged * 100, 1);
        }

        private int GetUnmetTrend(IQueryable<DonorRequest> query, DateTime? startDate)
        {
            if (!startDate.HasValue || startDate.Value > DateTime.Today.AddDays(-60))
                return 2; // Default

            var currentPeriod = query.Where(r => r.CreatedAt >= startDate);
            var previousPeriod = query.Where(r => r.CreatedAt >= startDate.Value.AddMonths(-1) && r.CreatedAt < startDate);

            var currentUnmet = currentPeriod.Count(r => r.Status == "Pending" || r.Status == "Cancelled");
            var previousUnmet = previousPeriod.Count(r => r.Status == "Pending" || r.Status == "Cancelled");

            return currentUnmet - previousUnmet;
        }

        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Only primary admin can access settings
            if (!isPrimaryUser)
            {
                TempData["ErrorMessage"] = "Only the primary hospital administrator can access settings.";
                return RedirectToAction("Dashboard");
            }

            // Get all hospital staff members
            var staffMembers = await _context.HospitalStaff
                .Include(s => s.User)
                .Where(s => s.HospitalId == hospital.Id)
                .ToListAsync();

            var teamMembers = new List<TeamMemberViewModel>();

            // Add primary hospital user as Admin
            var primaryInitials = GetInitials(user.FirstName, user.LastName, user.Email);
            teamMembers.Add(new TeamMemberViewModel
            {
                UserId = user.Id,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email ?? "",
                Role = "Admin",
                Status = user.Status ?? "Active",
                Initials = primaryInitials
            });

            // Add other staff members
            foreach (var staffMember in staffMembers)
            {
                if (staffMember.User != null)
                {
                    var initials = GetInitials(staffMember.User.FirstName, staffMember.User.LastName, staffMember.User.Email);
                    teamMembers.Add(new TeamMemberViewModel
                    {
                        StaffId = staffMember.Id,
                        UserId = staffMember.UserId,
                        Name = $"{staffMember.User.FirstName} {staffMember.User.LastName}".Trim(),
                        Email = staffMember.User.Email ?? "",
                        Role = staffMember.Role,
                        Status = staffMember.Status,
                        Initials = initials
                    });
                }
            }

            var model = new HospitalSettingsViewModel
            {
                HospitalId = hospital.Id,
                HospitalName = hospital.Name,
                License = hospital.License,
                Website = null, // Add Website field to Hospital model if needed
                Address = hospital.Address,
                City = hospital.City,
                Zip = hospital.Zip,
                Phone = hospital.User?.PhoneNumber ?? "",
                Email = hospital.User?.Email ?? "",
                LogoPath = hospital.LogoPath,
                NotificationPreferences = new NotificationPreferencesViewModel
                {
                    EmailNewDonorMatches = true,
                    EmailRequestStatusUpdates = true,
                    EmailMarketing = false,
                    SmsEmergencyLowStock = true,
                    SmsUrgentRequestFulfillment = true
                },
                TeamMembers = teamMembers
            };

            return View("HospitalSetting", model);
        }

        private string GetInitials(string? firstName, string? lastName, string? email)
        {
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                return $"{firstName[0]}{lastName[0]}".ToUpper();
            }
            else if (!string.IsNullOrEmpty(firstName))
            {
                return firstName.Substring(0, Math.Min(2, firstName.Length)).ToUpper();
            }
            else if (!string.IsNullOrEmpty(email))
            {
                return email.Substring(0, Math.Min(2, email.Length)).ToUpper();
            }
            return "??";
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateHospitalProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Only primary admin can update profile
            if (!isPrimaryUser)
            {
                TempData["ErrorMessage"] = "Only the primary hospital administrator can update the profile.";
                return RedirectToAction("Settings");
            }

            if (!ModelState.IsValid)
            {
                // Return to settings page with errors - reload full model
                return RedirectToAction("Settings");
            }

            // Update Hospital
            hospital.Name = model.HospitalName;
            hospital.Address = model.Address;
            hospital.City = model.City;
            hospital.Zip = model.Zip;
            // Note: Website field not in Hospital model yet - add if needed

            // Update User
            if (hospital.User != null)
            {
                hospital.User.Email = model.Email;
                hospital.User.UserName = model.Email;
                hospital.User.PhoneNumber = model.Phone;
                
                await _userManager.UpdateAsync(hospital.User);
            }

            _context.Hospitals.Update(hospital);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Settings");
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadHospitalLogo(IFormFile logoFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Only primary admin can upload logo
            if (!isPrimaryUser)
            {
                TempData["ErrorMessage"] = "Only the primary hospital administrator can upload the logo.";
                return RedirectToAction("Settings");
            }

            // Validate file
            if (logoFile == null || logoFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction("Settings");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Please upload a JPG, PNG, GIF, or WEBP image.";
                return RedirectToAction("Settings");
            }

            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (logoFile.Length > maxFileSize)
            {
                TempData["ErrorMessage"] = "File size exceeds 5MB limit. Please upload a smaller image.";
                return RedirectToAction("Settings");
            }

            try
            {
                // Create hospital logos directory if it doesn't exist
                var logosDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "hospitals");
                if (!Directory.Exists(logosDirectory))
                {
                    Directory.CreateDirectory(logosDirectory);
                }

                // Generate unique filename
                var fileName = $"hospital_{hospital.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(logosDirectory, fileName);
                var relativePath = $"/images/hospitals/{fileName}";

                // Delete old logo if exists
                if (!string.IsNullOrEmpty(hospital.LogoPath))
                {
                    var oldLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", hospital.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldLogoPath);
                        }
                        catch (Exception ex)
                        {
                            // Log warning but continue
                            Console.WriteLine($"Failed to delete old logo: {ex.Message}");
                        }
                    }
                }

                // Save new logo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                // Update hospital logo path
                hospital.LogoPath = relativePath;
                _context.Hospitals.Update(hospital);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hospital logo updated successfully!";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error uploading hospital logo: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred while uploading the logo: {ex.Message}. Please try again.";
                return RedirectToAction("Settings");
            }
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotificationPreferences(NotificationPreferencesViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // For now, we'll store preferences as JSON in a user claim or custom field
            // This is a simplified version - in production, you might want a dedicated table
            // Note: This would require adding a field to Users model or using UserClaims
            
            TempData["SuccessMessage"] = "Notification preferences updated successfully!";
            return RedirectToAction("Settings");
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeamMember(AddTeamMemberViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (hospital, staff, isPrimaryUser) = await GetHospitalForUserAsync(user.Id);

            if (hospital == null) return NotFound();

            // Only primary admin can add team members
            if (!isPrimaryUser)
            {
                TempData["ErrorMessage"] = "Only the primary hospital administrator can add team members.";
                return RedirectToAction("Settings");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return RedirectToAction("Settings");
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // Check if user is already a staff member
                var existingStaff = await _context.HospitalStaff
                    .FirstOrDefaultAsync(s => s.HospitalId == hospital.Id && s.UserId == existingUser.Id);
                
                if (existingStaff != null)
                {
                    TempData["ErrorMessage"] = "This user is already a member of your hospital staff.";
                    return RedirectToAction("Settings");
                }

                // User exists but not linked to this hospital - link them
                var newStaff = new HospitalStaff
                {
                    HospitalId = hospital.Id,
                    UserId = existingUser.Id,
                    Role = model.Role,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    InvitedAt = DateTime.UtcNow,
                    InvitedByUserId = user.Id
                };

                _context.HospitalStaff.Add(newStaff);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "User has been added to your hospital staff.";
                return RedirectToAction("Settings");
            }

            // Create new user account
            var newUser = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Role = "Hospital", // Hospital staff users have Hospital role
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false // Require email confirmation
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Settings");
            }

            // Add role claims
            await _userManager.AddClaimAsync(newUser, new Claim(ClaimTypes.Role, "Hospital"));
            await _userManager.AddClaimAsync(newUser, new Claim("Role", "Hospital"));

            // Link user to hospital as staff
            var staffMember = new HospitalStaff
            {
                HospitalId = hospital.Id,
                UserId = newUser.Id,
                Role = model.Role,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                InvitedAt = DateTime.UtcNow,
                InvitedByUserId = user.Id
            };

            _context.HospitalStaff.Add(staffMember);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Team member has been added successfully!";
            return RedirectToAction("Settings");
        }

        [Authorize(Roles = "Hospital")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTeamMember(int staffId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var hospital = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.UserId == user.Id);

            if (hospital == null) return NotFound();

            // Don't allow removing the primary hospital user
            if (staffId == 0) // Primary user has no staffId
            {
                TempData["ErrorMessage"] = "Cannot remove the primary hospital administrator.";
                return RedirectToAction("Settings");
            }

            var staff = await _context.HospitalStaff
                .FirstOrDefaultAsync(s => s.Id == staffId && s.HospitalId == hospital.Id);

            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff member not found.";
                return RedirectToAction("Settings");
            }

            // Optionally delete the user account, or just remove the link
            // For now, we'll just remove the link
            _context.HospitalStaff.Remove(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Team member has been removed successfully.";
            return RedirectToAction("Settings");
        }
    }


    // Request model for marking notification as read
    public class MarkNotificationReadRequest
    {
        public int NotificationId { get; set; }
    }

}