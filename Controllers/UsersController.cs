using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Claims;

namespace BloodDonation.Controllers
{
    public class UsersController : Controller
    {
        private readonly BloodDonationContext _context;
        private readonly UserManager<Users> _userManager;

        public UsersController(BloodDonationContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        //GET: Load donor profile into form
        [HttpGet("Users/DonorProfile/{id}")]
        public async Task<IActionResult> DonorProfile(int id)
        {
            var donor = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == id);

            if (donor == null)
                return NotFound();

            var model = new EditDonorProfileViewModel
            {
                FirstName = donor.User.FirstName,
                LastName = donor.User.LastName,
                BloodTypeId = donor.BloodTypeId,
                LocationId = donor.LocationId,
                Email = donor.User.Email,
                Phone = donor.User.PhoneNumber,
                IsHealthyForDonation = donor.IsHealthyForDonation,
                IsIdentityHidden = donor.IsIdentityHidden,
                IsAvailable = donor.IsAvailable,
                DateOfBirth = donor.DateOfBirth,
                Gender = donor.Gender
            };

            ViewBag.BloodTypes = _context.BloodTypes.ToList();
            ViewBag.Locations = _context.Locations.ToList();

            return View("DonorProfile", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfilePartial()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var donor = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null)
                return NotFound();

            var model = new EditDonorProfileViewModel
            {
                FirstName = donor.User.FirstName,
                LastName = donor.User.LastName,
                BloodTypeId = donor.BloodTypeId,
                Email = donor.User.Email,
                LocationId = donor.LocationId,
                DateOfBirth = donor.DateOfBirth,
                IsHealthyForDonation = donor.IsHealthyForDonation,
                IsIdentityHidden = donor.IsIdentityHidden,
                IsAvailable = donor.IsAvailable
            };

            ViewBag.BloodTypes = _context.BloodTypes.ToList() ?? new List<BloodTypes>();
            ViewBag.Locations = _context.Locations.ToList() ?? new List<Locations>();

            return PartialView("~/Views/Shared/_EditDonorProfile.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditDonorProfileViewModel model)
        {
            // 1️⃣ Get current user ID as int
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // 2️⃣ Check email uniqueness in Identity
            var emailExists = await _userManager.Users
                .AnyAsync(u => u.Email == model.Email && u.Id != userId);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already in use.");
            }

            // 3️⃣ Return partial if validation fails
            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = _context.BloodTypes.ToList();
                ViewBag.Locations = _context.Locations.ToList();
                return PartialView("_EditDonorProfile", model);
            }

            // 4️⃣ Update Identity user
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email; // optional: if username = email
            await _userManager.UpdateAsync(user);

            // 5️⃣ Update donor profile
            var donor = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null) return NotFound();

            donor.User.FirstName = model.FirstName;
            donor.User.LastName = model.LastName;
            donor.BloodTypeId = model.BloodTypeId;
            donor.LocationId = model.LocationId;
            donor.IsHealthyForDonation = model.IsHealthyForDonation;
            donor.IsIdentityHidden = model.IsIdentityHidden;
            donor.IsAvailable = model.IsAvailable;
            donor.DateOfBirth = model.DateOfBirth;

            await _context.SaveChangesAsync();

            // 6️⃣ Return JSON for AJAX redirect
            return Json(new
            {
                success = true,
                redirectUrl = Url.Action(nameof(DonorProfile), new { id = donor.DonorId })
            });
        }




        // ✅ TOGGLE: Availability (Available ↔ Unavailable)
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var donor = await _context.DonorProfile
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null)
                return NotFound();

            donor.IsAvailable = !donor.IsAvailable;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(DonorProfile), new { id = donor.DonorId });
        }

        // ✅ TOGGLE: Identity Visibility (Hidden ↔ Visible)
        [HttpPost]
        public async Task<IActionResult> ToggleIdentity()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var donor = await _context.DonorProfile
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null)
                return NotFound();

            donor.IsIdentityHidden = !donor.IsIdentityHidden;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(DonorProfile), new { id = donor.DonorId });
        }
    }
}
