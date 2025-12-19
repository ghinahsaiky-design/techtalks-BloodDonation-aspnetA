using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    [Authorize(Roles = "Owner")]
    public class AdminManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
