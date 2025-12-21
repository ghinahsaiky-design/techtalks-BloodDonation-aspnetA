using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Controllers
{
    public class UsersController : Controller
    {
        private readonly BloodDonationContext _context;

        public UsersController(BloodDonationContext context)
        {
            _context = context;
        }

        // GET (loads data into the div)
        [HttpGet]
        public async Task<IActionResult> EditProfile()
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
                LocationId = donor.LocationId,
                IsHealthyForDonation = donor.IsHealthyForDonation,
                IsIdentityHidden = donor.IsIdentityHidden,
                DateOfBirth = donor.DateOfBirth,
                Gender = donor.Gender
            };

            ViewBag.BloodTypes = _context.BloodTypes.ToList();
            ViewBag.Locations = _context.Locations.ToList();

            return PartialView("_EditDonorProfilePartial", model);
        }

        // POST (save changes)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditDonorProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = _context.BloodTypes.ToList();
                ViewBag.Locations = _context.Locations.ToList();
                return PartialView("_EditDonorProfilePartial", model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var donor = await _context.DonorProfile
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonorId == userId);

            if (donor == null)
                return NotFound();

            // UPDATE USER
            donor.User.FirstName = model.FirstName;
            donor.User.LastName = model.LastName;

            // UPDATE DONOR PROFILE
            donor.BloodTypeId = model.BloodTypeId;
            donor.LocationId = model.LocationId;
            donor.IsHealthyForDonation = model.IsHealthyForDonation;
            donor.IsIdentityHidden = model.IsIdentityHidden;
            donor.DateOfBirth = model.DateOfBirth;
            donor.Gender = model.Gender;

            await _context.SaveChangesAsync();

            ViewBag.Success = "Profile updated successfully.";

            ViewBag.BloodTypes = _context.BloodTypes.ToList();
            ViewBag.Locations = _context.Locations.ToList();

            return PartialView("_EditDonorProfilePartial", model);
        }
    }
}
