using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult SearchDonors()
        {
            return View();
        }
    }
}
