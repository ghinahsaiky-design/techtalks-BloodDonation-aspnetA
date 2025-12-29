using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.ViewComponents
{
    public class HospitalSidebarViewComponent : ViewComponent
    {
        private readonly UserManager<Users> _userManager;
        private readonly BloodDonationContext _context;

        public HospitalSidebarViewComponent(UserManager<Users> userManager, BloodDonationContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentAction = ViewContext.RouteData.Values["action"]?.ToString() ?? "";
            var user = await _userManager.GetUserAsync(HttpContext.User);
            
            // Get user permissions
            bool isPrimaryUser = false;
            bool canCreateRequests = false;
            bool canAccessSettings = false;
            string userRole = "Staff";
            string firstName = user?.FirstName ?? "";
            string? logoPath = null;
            string? hospitalName = null;
            
            if (user != null)
            {
                // Check if user is primary hospital user
                var hospital = await _context.Hospitals
                    .FirstOrDefaultAsync(h => h.UserId == user.Id);
                
                isPrimaryUser = hospital != null;
                
                if (isPrimaryUser)
                {
                    userRole = "Admin";
                    canCreateRequests = true;
                    canAccessSettings = true;
                    logoPath = hospital?.LogoPath;
                    hospitalName = hospital?.Name;
                }
                else
                {
                    // Check if user is a staff member
                    var staff = await _context.HospitalStaff
                        .Include(s => s.Hospital)
                        .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == "Active");
                    
                    if (staff != null)
                    {
                        userRole = staff.Role;
                        canCreateRequests = staff.Role == "Admin" || staff.Role == "Coordinator";
                        canAccessSettings = false; // Only primary admin can access settings
                        logoPath = staff.Hospital?.LogoPath;
                        hospitalName = staff.Hospital?.Name;
                    }
                }
            }

            var model = new HospitalSidebarViewModel
            {
                CurrentAction = currentAction,
                FirstName = firstName,
                UserRole = userRole,
                CanCreateRequests = canCreateRequests,
                CanAccessSettings = canAccessSettings,
                LogoPath = logoPath,
                HospitalName = hospitalName
            };

            return View(model);
        }
    }

    public class HospitalSidebarViewModel
    {
        public string CurrentAction { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string UserRole { get; set; } = "Staff";
        public bool CanCreateRequests { get; set; }
        public bool CanAccessSettings { get; set; }
        public string? LogoPath { get; set; }
        public string? HospitalName { get; set; }
    }
}

