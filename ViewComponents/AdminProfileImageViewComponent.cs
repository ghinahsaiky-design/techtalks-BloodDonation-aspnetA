using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Models;

namespace BloodDonation.ViewComponents
{
    public class AdminProfileImageViewComponent : ViewComponent
    {
        private readonly UserManager<Users> _userManager;

        public AdminProfileImageViewComponent(UserManager<Users> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (ViewContext.HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(ViewContext.HttpContext.User);
                if (user != null && !string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    return View("Default", user.ProfileImagePath);
                }
            }
            return View("Default", (string?)null);
        }
    }
}

