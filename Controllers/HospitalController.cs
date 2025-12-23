using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Controllers
{
    [Authorize(Roles = "Owner")]
    public class HospitalController : Controller
    {
        private readonly BloodDonationContext _context;
        private readonly UserManager<Users> _userManager;

        public HospitalController(BloodDonationContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Hospital Management List
        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var list = _context.Hospitals.Include(h => h.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                list = list.Where(h =>
                    h.Name.ToLower().Contains(lower) ||
                    h.User.Email.ToLower().Contains(lower) ||
                    h.License.ToLower().Contains(lower));
            }

            // Order by User.CreatedAt Descending
            var model = await list.OrderByDescending(h => h.User.CreatedAt).ToListAsync();
            
            return View("~/Views/Owner/HospitalManagement.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddNewHospitalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return PartialView("~/Views/Owner/AddNewHospital.cshtml", model);
            }

            // 1. Create User Account
            var user = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Name, // Using hospital name as first name for simplicity
                LastName = "Hospital",
                PhoneNumber = model.Phone,
                Role = "Hospital",
                Status = model.Status ?? "Active",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Response.StatusCode = 400;
                return PartialView("~/Views/Owner/AddNewHospital.cshtml", model);
            }

            // Add role claims
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Hospital"));
            await _userManager.AddClaimAsync(user, new Claim("Role", "Hospital"));

            // 2. Create Hospital Entity
            var entity = new Hospital
            {
                Name = model.Name,
                License = model.License,
                UserId = user.Id,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Zip = model.Zip
            };

            _context.Hospitals.Add(entity);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();

            var vm = new AddNewHospitalViewModel
            {
                Name = hospital.Name,
                License = hospital.License,
                Email = hospital.User?.Email ?? "",
                Phone = hospital.User?.PhoneNumber ?? "",
                Address = hospital.Address,
                City = hospital.City,
                State = hospital.State,
                Zip = hospital.Zip,
                Status = hospital.User?.Status ?? "Active"
            };

            ViewData["HospitalId"] = id;
            return PartialView("~/Views/Owner/_EditHospitalModal.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> DetailsModal(int id)
        {
            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();

            return PartialView("~/Views/Owner/_HospitalDetailsModal.cshtml", hospital);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddNewHospitalViewModel model)
        {
            // Password is not required for editing
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                ViewData["HospitalId"] = id;
                Response.StatusCode = 400;
                return PartialView("~/Views/Owner/_EditHospitalModal.cshtml", model);
            }

            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();

            // Update Hospital Entity
            hospital.Name = model.Name;
            hospital.License = model.License;
            hospital.Address = model.Address;
            hospital.City = model.City;
            hospital.State = model.State;
            hospital.Zip = model.Zip;

            // Update User Account
            if (hospital.User != null)
            {
                hospital.User.Email = model.Email;
                hospital.User.UserName = model.Email;
                hospital.User.PhoneNumber = model.Phone;
                hospital.User.Status = model.Status ?? "Active";
                hospital.User.FirstName = model.Name;
                
                await _userManager.UpdateAsync(hospital.User);
            }

            _context.Hospitals.Update(hospital);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hospital = await _context.Hospitals.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == id);
            if (hospital != null)
            {
                var user = hospital.User;
                _context.Hospitals.Remove(hospital);
                
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
                
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("HospitalManagement", "Owner");
        }
    }
}
