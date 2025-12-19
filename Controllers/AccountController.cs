using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public IActionResult Profile()
        {
            // Donors use a different view (DonorProfile) with the standard layout
            if (User.IsInRole("Donor") && !User.IsInRole("Admin") && !User.IsInRole("Owner"))
            {
                return View("DonorProfile");
            }
            
            // Admins and Owners use the Profile view with DashboardLayout
            return View();
        }
    }
}
