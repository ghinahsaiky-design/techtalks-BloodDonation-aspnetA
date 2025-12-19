using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;

namespace BloodDonation.Controllers
{
    public class DashboardController : Controller
    {
        private readonly BloodDonationContext _context;

        public DashboardController(BloodDonationContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchDonors(SearchViewModel model)
        {

            // Fill dropdowns
            model.Locations = await _context.Locations
                .OrderBy(l => l.Districts)
                .Select(l => new SelectListItem
                {
                    Value = l.LocationId.ToString(),
                    Text = l.Districts
                }).ToListAsync();

            model.BloodTypes = await _context.BloodTypes
                .OrderBy(b => b.Type)
                .Select(b => new SelectListItem
                {
                    Value = b.BloodTypeId.ToString(),
                    Text = b.Type
                }).ToListAsync();

            // Build query - Only show users with "Donor" role
            var query = _context.DonorProfile
                .Where(d => d.User.Role == "Donor")
                .AsQueryable();

            if (model.SelectedLocationId.HasValue)
                query = query.Where(d => d.LocationId == model.SelectedLocationId.Value);

            if (model.SelectedBloodTypeId.HasValue)
                query = query.Where(d => d.BloodTypeId == model.SelectedBloodTypeId.Value);


            model.Results = await query.OrderBy(d => !d.IsHealthyForDonation)
                .Select(d => new SearchResultViewModel
                {
                    DonorName = d.IsIdentityHidden ? $"Donor #{d.DonorId}" : d.User.FirstName + " " + d.User.LastName,
                    BloodType = d.BloodType.Type,
                    City = d.Location.Districts,
                    PhoneNumber = d.User.PhoneNumber,
                    Email = d.User.Email,
                    IsHealthy = d.IsHealthyForDonation,
                    IsAvailable = d.IsAvailable
                }).ToListAsync();

            return View(model);
        }

    }
}
