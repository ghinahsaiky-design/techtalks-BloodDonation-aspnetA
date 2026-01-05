using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace BloodDonation.ViewComponents
{
    public class UserProfileImageViewComponent : ViewComponent
    {
        private readonly UserManager<Users> _userManager;

        public UserProfileImageViewComponent(UserManager<Users> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string profileImagePath = "";
            string firstLetter = "";
            
            if (User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
                if (user != null)
                {
                    profileImagePath = user.ProfileImagePath ?? "";
                    firstLetter = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName[0].ToString().ToUpper() : "";
                }
            }

            string html;
            if (!string.IsNullOrEmpty(profileImagePath))
            {
                html = $@"
                    <a href=""/Account/Profile"" class=""relative group flex h-10 w-10 items-center justify-center rounded-full overflow-hidden"">
                        <img src=""{profileImagePath}"" alt=""Profile"" class=""w-full h-full object-cover"" />
                        <span class=""absolute top-12 text-sm font-medium text-soft-red opacity-0 transition-opacity group-hover:opacity-100 whitespace-nowrap"">View profile</span>
                    </a>";
            }
            else
            {
                html = $@"
                    <a href=""/Account/Profile"" class=""relative group flex h-10 w-10 items-center justify-center rounded-full bg-soft-red text-white text-sm font-semibold"">
                        {firstLetter}
                        <span class=""absolute top-12 text-sm font-medium text-soft-red opacity-0 transition-opacity group-hover:opacity-100 whitespace-nowrap"">View profile</span>
                    </a>";
            }

            return new HtmlContentViewComponentResult(new HtmlString(html));
        }
    }
}
