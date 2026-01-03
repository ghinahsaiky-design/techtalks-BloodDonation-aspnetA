using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Models;
using BloodDonation.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly BloodDonationContext _context;

        public AccountController(UserManager<Users> userManager, BloodDonationContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check if user is a hospital user (primary or staff)
            var isHospitalUser = await _context.Hospitals
                .AnyAsync(h => h.UserId == user.Id);
            
            var isHospitalStaff = await _context.HospitalStaff
                .AnyAsync(s => s.UserId == user.Id && s.Status == "Active");

            if (isHospitalUser || isHospitalStaff)
            {
                return RedirectToAction("Dashboard", "Hospital");
            }

            if (user.Role == "Donor")
            {
                return RedirectToAction("DonorProfile", "Users", new { id = user.Id });
            }

            // Admins and Owners use the Admin/Profile view
            return RedirectToAction("Profile", "Admin");
        }

        public IActionResult Settings()
        {
            // Admins and Owners use the Admin/Settings view
            return RedirectToAction("Settings", "Admin");
        }
    }
}
