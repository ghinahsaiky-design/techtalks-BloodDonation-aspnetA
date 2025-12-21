using BloodDonation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly UserManager<Users> _userManager;

        public OwnerController(UserManager<Users> userManager)
        {
            _userManager = userManager;
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

            // All Data Operations will be implemented here

            ViewBag.OwnerName = user.FirstName + " " + user.LastName;

            return View();
        }

        // Admins tab
        public async Task<IActionResult> AdminManagement()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            return View();
        }

        // Hospital tab
        public async Task<IActionResult> HospitalManagement()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            return View();
        }

    }
}