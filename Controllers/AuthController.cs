using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
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

            // Check user status before attempting login (for Hospital/Admin users)
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Check if account is pending
                if ((user.Role == "Hospital" || user.Role == "Admin") && user.Status == "Pending")
                {
                    // Don't reveal whether the password is correct if account is pending
                    // This prevents information disclosure
                    ModelState.Remove(nameof(LoginViewModel.Email));
                    ModelState.Remove(nameof(LoginViewModel.Password));
                    model.Email = string.Empty;
                    model.Password = string.Empty;
                    ModelState.AddModelError("", "Your account is pending approval. Please wait for administrator approval before logging in.");
                    return View(model);
                }

                // Check if Hospital user is still linked to a hospital
                if (user.Role == "Hospital")
                {
                    // Check if user is a primary hospital user
                    var isPrimaryHospitalUser = await _context.Hospitals
                        .AnyAsync(h => h.UserId == user.Id);

                    // Check if user is an active staff member
                    var isActiveStaff = await _context.HospitalStaff
                        .AnyAsync(s => s.UserId == user.Id && s.Status == "Active");

                    // If user is not a primary hospital user and not an active staff member, prevent login
                    if (!isPrimaryHospitalUser && !isActiveStaff)
                    {
                        // Don't reveal whether the password is correct
                        ModelState.Remove(nameof(LoginViewModel.Email));
                        ModelState.Remove(nameof(LoginViewModel.Password));
                        model.Email = string.Empty;
                        model.Password = string.Empty;
                        ModelState.AddModelError("", "Your account is no longer associated with a hospital. Please contact your hospital administrator.");
                        return View(model);
                    }
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (user == null)
                    user = await _userManager.FindByEmailAsync(model.Email);

                // Double-check hospital association after password verification
                if (user != null && user.Role == "Hospital")
                {
                    var isPrimaryHospitalUser = await _context.Hospitals
                        .AnyAsync(h => h.UserId == user.Id);

                    var isActiveStaff = await _context.HospitalStaff
                        .AnyAsync(s => s.UserId == user.Id && s.Status == "Active");

                    if (!isPrimaryHospitalUser && !isActiveStaff)
                    {
                        // Sign out immediately if they somehow got past the first check
                        await _signInManager.SignOutAsync();
                        ModelState.Remove(nameof(LoginViewModel.Email));
                        ModelState.Remove(nameof(LoginViewModel.Password));
                        model.Email = string.Empty;
                        model.Password = string.Empty;
                        ModelState.AddModelError("", "Your account is no longer associated with a hospital. Please contact your hospital administrator.");
                        return View(model);
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim("FirstName", user.FirstName),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);

                // Redirect based on user role
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
                    return RedirectToAction("Dashboard", "Hospital");
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

        [HttpGet]
        public IActionResult ChangePasswordPartial()
        { 
            return PartialView("_ChangePasswordPartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_ChangePasswordPartial", model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Unauthorized();

            // Check old password first
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!passwordValid)
            {
                ModelState.AddModelError("OldPassword", "Incorrect password.");
                return PartialView("_ChangePasswordPartial", model);
            }

            // Attempt to change password
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                var claims = new List<Claim>
        {
            new Claim("FirstName", user.FirstName ?? "")
        };
                await _signInManager.SignInWithClaimsAsync(user, false, claims);

                var donor = await _context.DonorProfile
                    .FirstOrDefaultAsync(d => d.DonorId == user.Id);

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("DonorProfile", "Users", new { id = donor?.DonorId })
                });
            }

            // Add any Identity errors
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return PartialView("_ChangePasswordPartial", model);
        }



        public IActionResult DeleteAccountPartial()
        {
            return PartialView("_DeleteAccountPartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_DeleteAccountPartial", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var passwordValid =
                await _userManager.CheckPasswordAsync(user, model.Password);

            if (!passwordValid)
            {
                ModelState.AddModelError("Password", "Incorrect password.");
                return PartialView("_DeleteAccountPartial", model);
            }

            await _signInManager.SignOutAsync();
            await _userManager.DeleteAsync(user);

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("Index", "Home")
            });
        }



    }
}
