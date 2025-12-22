using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BloodDonation.Controllers
{
    public class UsersController : Controller
    {
        private readonly BloodDonationContext _context;

        public UsersController(BloodDonationContext context)
        {
            _context = context;
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


        // ✅ POST: Save profile changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDonorProfile(EditDonorProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = _context.BloodTypes.ToList();
                ViewBag.Locations = _context.Locations.ToList();
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var donor = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null)
                return NotFound();

            // Update user fields
            donor.User.FirstName = model.FirstName;
            donor.User.LastName = model.LastName;

            // Update donor fields
            donor.BloodTypeId = model.BloodTypeId;
            donor.LocationId = model.LocationId;
            donor.IsHealthyForDonation = model.IsHealthyForDonation;
            donor.IsIdentityHidden = model.IsIdentityHidden;
            donor.IsAvailable = model.IsAvailable;
            donor.DateOfBirth = model.DateOfBirth;
            donor.Gender = model.Gender;

            await _context.SaveChangesAsync();

            ViewBag.Success = "Profile updated successfully.";
            ViewBag.BloodTypes = _context.BloodTypes.ToList();
            ViewBag.Locations = _context.Locations.ToList();

            return View(model);
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

            TempData["ProfileMessage"] = donor.IsAvailable
                ? "✅ You are now available to donate."
                : "❌ You are now marked as unavailable.";

            return RedirectToAction(nameof(EditDonorProfile));
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

            TempData["ProfileMessage"] = donor.IsIdentityHidden
                ? "🕵️ Your identity is now hidden."
                : "👤 Your identity is now visible.";

            return RedirectToAction(nameof(EditDonorProfile));
        }
    }
}
