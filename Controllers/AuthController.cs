using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;



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

            // Check if the email is already registered
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                model.Locations = _context.Locations.ToList();
                model.BloodTypes = _context.BloodTypes.ToList();
                return View(model);
            }

            var user = new Users
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.Phone,
                Role="Donor"
            };
            var claims = new List<Claim>
            {
                new Claim("FirstName",user.FirstName),
                new Claim(ClaimTypes.Role, user.Role)
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
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    IsHealthyForDonation = model.IsHealthyForDonation,
                    IsIdentityHidden = model.IsIdentityHidden,
                    IsAvailable = true,
                    LastDonationDate = null
                };

                _context.DonorProfile.Add(donor);
                await _context.SaveChangesAsync();

                // Auto login
                await _signInManager.SignInWithClaimsAsync(user, isPersistent: false,claims);

                return RedirectToAction("SearchDonors", "Dashboard");
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


        // ----------------------
        // LOGIN (GET)
        // ----------------------
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ----------------------
        // LOGIN (POST)
        // ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                var claims = new List<Claim>
                {
                    new Claim("FirstName", user.FirstName),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);

                // Redirect admin users to admin dashboard
                if (user.Role == "Owner")
                {
                    return RedirectToAction("Index", "Owner");
                }
                else if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if(user.Role == "Hospital")
                {
                    return RedirectToAction("Index", "Hospital");
                }
                else
                {
                    return RedirectToAction("SearchDonors", "Dashboard");
                }
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked. Try again later.");
                return View(model);
            }
        
            ModelState.Remove(nameof(LoginViewModel.Email));
            ModelState.Remove(nameof(LoginViewModel.Password));
            model.Email = string.Empty;
            model.Password = string.Empty;
            ModelState.AddModelError("Password", "Invalid login attempt.");
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
