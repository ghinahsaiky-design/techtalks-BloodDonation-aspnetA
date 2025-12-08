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
        public IActionResult SearchDonors()
        {
            var model = new SearchViewModel();

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

            return View(model);
        }
    }
}
