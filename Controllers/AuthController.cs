using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    public class AuthController : Controller
    {
        // other actions...

        [HttpGet]
        public IActionResult SignUp()
        {
            var model = new RegisterViewModel();
            return View(model);
        }

        // Your friend will later add the POST:
        // [HttpPost]
        // public async Task<IActionResult> SignUp(RegisterViewModel model) { ... }
    }
}
