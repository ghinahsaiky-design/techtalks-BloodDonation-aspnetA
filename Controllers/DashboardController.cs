using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public IActionResult SearchDonors(SearchViewModel model)
        {

            // Fill dropdowns
            model.Locations = _context.Locations
                .OrderBy(l => l.Districts)
                .Select(l => new SelectListItem
                {
                    Value = l.LocationId.ToString(),
                    Text = l.Districts
                });

            model.BloodTypes = _context.BloodTypes
                .OrderBy(b => b.Type)
                .Select(b => new SelectListItem
                {
                    Value = b.BloodTypeId.ToString(),
                    Text = b.Type
                });

            // Build query
            var query = _context.DonorProfile.AsQueryable();

            if (model.SelectedLocationId.HasValue)
                query = query.Where(d => d.LocationId == model.SelectedLocationId.Value);

            if (model.SelectedBloodTypeId.HasValue)
                query = query.Where(d => d.BloodTypeId == model.SelectedBloodTypeId.Value);


            model.Results = query
                .Select(d => new SearchResultViewModel
                {
                    DonorId = d.DonorId,
                    DonorName = d.User.FirstName + " " + d.User.LastName,
                    BloodType = d.BloodType.Type,
                    City = d.Location.Districts,
                    PhoneNumber = d.User.Phone,
                    Email = d.User.Email,
                    IsHealthy = d.IsHealthyForDonation,
                    IsIdentityHidden = d.IsIdentityHidden,
                    IsAvailable = d.IsAvailable
                }).ToList();

            return View(model);
        }

    }
}
