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

        // Dashboard
        public async Task<IActionResult> Index()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.OwnerName = user.FirstName + " " + user.LastName;

            return View();
        }

        // Admins tab
        public async Task<IActionResult> Admins()
        {
            if (!await IsOwnerAsync())
                return Forbid();

            return View("~/Views/Owner/AdminManagement.cshtml");
        }

    }
}