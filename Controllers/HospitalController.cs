using System;
using System.Linq;
using System.Threading.Tasks;
using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Controllers
{
    public class HospitalController : Controller
    {
        private readonly BloodDonationContext _context;

        public HospitalController(BloodDonationContext context)
        {
            _context = context;
        }

        // Reuse your existing view: Views/Owner/HospitalManagement.cshtml
        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var list = _context.Hospitals.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                list = list.Where(h =>
                    h.Name.ToLower().Contains(lower) ||
                    h.Email.ToLower().Contains(lower) ||
                    h.License.ToLower().Contains(lower));
            }

            // Order by CreatedAt Descending to match OwnerController
            var model = await list.OrderByDescending(h => h.CreatedAt).ToListAsync();
            return View("~/Views/Owner/HospitalManagement.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddNewHospitalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Validation failed. Please fix errors and try again.");
                // Retain existing data
                var list = await _context.Hospitals.OrderByDescending(h => h.CreatedAt).ToListAsync();
                return View("~/Views/Owner/HospitalManagement.cshtml", list);
            }

            var entity = new Hospital
            {
                Name = model.Name,
                License = model.License,
                ContactPerson = model.ContactPerson,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Zip = model.Zip,
                // hash in production
                PasswordHash = model.Password,
                CreatedAt = DateTime.UtcNow
            };

            _context.Hospitals.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction("HospitalManagement", "Owner");
        }

        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();

            // Map for editing
            var vm = new AddNewHospitalViewModel
            {
                Name = hospital.Name,
                License = hospital.License,
                ContactPerson = hospital.ContactPerson,
                Email = hospital.Email,
                Phone = hospital.Phone,
                Address = hospital.Address,
                City = hospital.City,
                State = hospital.State,
                Zip = hospital.Zip
            };

            ViewData["HospitalId"] = id;
            return PartialView("~/Views/Owner/_EditHospitalModal.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();

            // Map for editing
            var vm = new AddNewHospitalViewModel
            {
                Name = hospital.Name,
                License = hospital.License,
                ContactPerson = hospital.ContactPerson,
                Email = hospital.Email,
                Phone = hospital.Phone,
                Address = hospital.Address,
                City = hospital.City,
                State = hospital.State,
                Zip = hospital.Zip
                // Password not returned
            };

            ViewData["HospitalId"] = id;
            return View("~/Views/Owner/EditHospital.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
             var hospital = await _context.Hospitals.FindAsync(id);
             if (hospital == null) return NotFound();

             return PartialView("~/Views/Owner/_HospitalDetailsModal.cshtml", hospital);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddNewHospitalViewModel model)
        {
            // Password is not required during edit
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                ViewData["HospitalId"] = id;
                Response.StatusCode = 400; // Bad Request
                return PartialView("~/Views/Owner/_EditHospitalModal.cshtml", model);
            }

            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();

            hospital.Name = model.Name;
            hospital.License = model.License;
            hospital.ContactPerson = model.ContactPerson;
            hospital.Email = model.Email;
            hospital.Phone = model.Phone;
            hospital.Address = model.Address;
            hospital.City = model.City;
            hospital.State = model.State;
            hospital.Zip = model.Zip;
            hospital.LastUpdatedAt = DateTime.UtcNow;

            // if password filled, update (hash in production)
            if (!string.IsNullOrWhiteSpace(model.Password))
                hospital.PasswordHash = model.Password;

            _context.Hospitals.Update(hospital);
            await _context.SaveChangesAsync();

            // Return success status so client knows to reload/redirect
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital != null)
            {
                _context.Hospitals.Remove(hospital);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("HospitalManagement", "Owner");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var h = await _context.Hospitals.FindAsync(id);
            if (h == null) return NotFound();

            return View("~/Views/Owner/HospitalDetails.cshtml", h);
        }
    }
}

