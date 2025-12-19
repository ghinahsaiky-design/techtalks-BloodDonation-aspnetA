using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    [Authorize]
    public class DonorController : Controller
    {
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult Index()
        {
            return View();
        }


    }
}
