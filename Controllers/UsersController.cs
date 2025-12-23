using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
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

        // ✅ POST: Save profile changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditDonorProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = _context.BloodTypes.ToList();
                ViewBag.Locations = _context.Locations.ToList();
                return PartialView("_EditDonorProfile", model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var donor = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null)
                return NotFound();

            donor.User.FirstName = model.FirstName;
            donor.User.LastName = model.LastName;
            donor.User.Email = model.Email;
            donor.BloodTypeId = model.BloodTypeId;
            donor.LocationId = model.LocationId;
            donor.IsHealthyForDonation = model.IsHealthyForDonation;
            donor.IsIdentityHidden = model.IsIdentityHidden;
            donor.IsAvailable = model.IsAvailable;
            donor.DateOfBirth = model.DateOfBirth;

            await _context.SaveChangesAsync();

            ViewBag.BloodTypes = _context.BloodTypes.ToList() ?? new List<BloodTypes>();
            ViewBag.Locations = _context.Locations.ToList() ?? new List<Locations>();

            return RedirectToAction(nameof(DonorProfile), new { id = donor.DonorId });
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
