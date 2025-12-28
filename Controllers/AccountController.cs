using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Models;
using System.Threading.Tasks;

namespace BloodDonation.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<Users> _userManager;

        public AccountController(UserManager<Users> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
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
