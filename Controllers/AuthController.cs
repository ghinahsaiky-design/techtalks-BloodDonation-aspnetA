using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace BloodDonation.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly BloodDonationContext _context;

        public AuthController(SignInManager<Users> signInManager, UserManager<Users> userManager, BloodDonationContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // ----------------------
        // SIGNUP (GET)
        // ----------------------
        [HttpGet]
        public IActionResult SignUp()
        {
            var model = new RegisterViewModel
            {
                Locations = _context.Locations.ToList(),
                BloodTypes = _context.BloodTypes.ToList()
            };

            return View(model);
        }

        // ----------------------
        // SIGNUP (POST)
        // ----------------------
        [HttpPost]
        public async Task<IActionResult> SignUp(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Locations = _context.Locations.ToList();
                model.BloodTypes = _context.BloodTypes.ToList();
                return View(model);
            }

            var user = new Users
            {
                FirstName = model.FullName,
                LastName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.Phone,
                Role="Donor"
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // CREATE DONOR PROFILE FOR THIS USER
                var donor = new DonorProfile
                {
                    DonorId = user.Id,  // FK to IdentityUser
                    BloodTypeId = model.BloodTypeId,
                    LocationId = model.LocationId,
                    Age = model.Age,
                    Gender = model.Gender,
                    IsHealthyForDonation = model.IsHealthyForDonation,
                    IsIdentityHidden = model.IsIdentityHidden,
                    IsAvailable = true,
                    LastDonationDate = null
                };

                _context.DonorProfile.Add(donor);
                await _context.SaveChangesAsync();

                // Auto login
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            // SHOW ERRORS
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.Locations = _context.Locations.ToList();
            model.BloodTypes = _context.BloodTypes.ToList();

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                await _signInManager.SignOutAsync();
            }

            return RedirectToAction("Index", "Home");
        }

    }
}
