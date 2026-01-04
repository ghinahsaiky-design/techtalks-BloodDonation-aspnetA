using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Data
{
    public class DataSeeder
    {
        private readonly UserManager<Users> _userManager;
        private readonly BloodDonationContext _context;

        public DataSeeder(UserManager<Users> userManager, BloodDonationContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task ReseedOwnerUsersAsync()
        {
            Console.WriteLine("Reseeding owner users...");
            
            // Get all owner users
            var ownerUsers = await _context.Users
                .Where(u => u.Role == "Owner")
                .ToListAsync();

            Console.WriteLine($"Found {ownerUsers.Count} existing owner users to delete.");

            // Delete all owner users
            foreach (var user in ownerUsers)
            {
                // Remove all claims first
                var claims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in claims)
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }
                
                // Delete the user
                var deleteResult = await _userManager.DeleteAsync(user);
                if (deleteResult.Succeeded)
                {
                    Console.WriteLine($"✓ Deleted owner user: {user.Email}");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to delete owner user {user.Email}: {string.Join(", ", deleteResult.Errors.Select(e => e.Description))}");
                }
            }

            // Now seed the owner users fresh
            await SeedOwnerUserAsync();
            Console.WriteLine("Owner users reseeding completed!");
        }

        public async Task SeedOwnerUserAsync()
        {
            var ownerUsers = new[]
            {
                new { Email = "nourh2235@gmail.com", FirstName = "Nour", LastName = "Hammoud", Password = "Admin@123" },
                new { Email = "Lewaamalaeb122@gmail.com", FirstName = "Liwaa", LastName = "Aljaramani", Password = "Admin@123" },
                new { Email = "Ghinahsaiky@gmail.com", FirstName = "Ghina", LastName = "Hsaiky", Password = "Admin@123" },
                new { Email = "rifaatramadan0@gmail.com", FirstName = "Rifaat", LastName = "Ramadan", Password = "Admin@123" },
                new { Email = "yamen_nasr@outlook.com", FirstName = "Yamen", LastName = "Nasr", Password = "Admin@123" },
                new { Email = "akhdarmohammad01@gmail.com", FirstName = "Mohammad", LastName = "Akhdar", Password = "Admin@123" },
                new { Email = "mahdi.m.tawbe@gmail.com", FirstName = "Mahdi", LastName = "Tawbe", Password = "Admin@123" }
            };

            foreach (var ownerData in ownerUsers)
            {
                // Check if owner already exists
                var ownerUser = await _userManager.FindByEmailAsync(ownerData.Email);

                if (ownerUser == null)
                {
                    ownerUser = new Users
                    {
                        FirstName = ownerData.FirstName,
                        LastName = ownerData.LastName,
                        Email = ownerData.Email,
                        UserName = ownerData.Email,
                        EmailConfirmed = true,
                        Role = "Owner",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(ownerUser, ownerData.Password);

                    if (result.Succeeded)
                    {
                        // Add role claim
                        var roleClaimResult = await _userManager.AddClaimAsync(ownerUser, new Claim(ClaimTypes.Role, "Owner"));
                        var customRoleClaimResult = await _userManager.AddClaimAsync(ownerUser, new Claim("Role", "Owner"));
                        
                        if (roleClaimResult.Succeeded && customRoleClaimResult.Succeeded)
                        {
                            Console.WriteLine($"✓ Owner user '{ownerData.Email}' (ID: {ownerUser.Id}) created successfully with password: {ownerData.Password} and role claims added");
                        }
                        else
                        {
                            var errors = roleClaimResult.Errors.Concat(customRoleClaimResult.Errors);
                            Console.WriteLine($"✗ Owner user '{ownerData.Email}' created but failed to add claims: {string.Join(", ", errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create owner user '{ownerData.Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // Check if user already has role claims, if not add them
                    var existingClaims = await _userManager.GetClaimsAsync(ownerUser);
                    var hasRoleClaim = existingClaims.Any(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "Owner"));
                    
                    if (!hasRoleClaim)
                    {
                        var userRole = ownerUser.Role ?? "Owner";
                        var roleClaimResult = await _userManager.AddClaimAsync(ownerUser, new Claim(ClaimTypes.Role, userRole));
                        var customRoleClaimResult = await _userManager.AddClaimAsync(ownerUser, new Claim("Role", userRole));
                        
                        if (roleClaimResult.Succeeded && customRoleClaimResult.Succeeded)
                        {
                            Console.WriteLine($"✓ Added role claims to existing owner user '{ownerData.Email}' (ID: {ownerUser.Id}) with role '{userRole}'");
                        }
                        else
                        {
                            var errors = roleClaimResult.Errors.Concat(customRoleClaimResult.Errors);
                            Console.WriteLine($"✗ Failed to add claims to existing owner user '{ownerData.Email}': {string.Join(", ", errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Owner user '{ownerData.Email}' already exists with role claims.");
                    }
                }
            }

            // Ensure all existing Owner users have role claims
            await EnsureOwnerClaimsAsync();
        }

        /// <summary>
        /// Ensures all users with Role = "Owner" have the appropriate role claims
        /// </summary>
        private async Task EnsureOwnerClaimsAsync()
        {
            try
            {
                var ownerUsersToProcess = await _context.Users
                    .Where(u => u.Role == "Owner")
                    .ToListAsync();

                Console.WriteLine($"Found {ownerUsersToProcess.Count} Owner users to process for claims.");

                foreach (var user in ownerUsersToProcess)
                {
                    try
                    {
                        var existingClaims = await _userManager.GetClaimsAsync(user);
                        var hasRoleClaim = existingClaims.Any(c => 
                            (c.Type == ClaimTypes.Role || c.Type == "Role") && 
                            (c.Value == "Owner"));

                        if (!hasRoleClaim)
                        {
                            // Add both ClaimTypes.Role and custom "Role" claim for compatibility
                            var roleClaimResult = await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, user.Role));
                            var customRoleClaimResult = await _userManager.AddClaimAsync(user, new Claim("Role", user.Role));
                            
                            if (roleClaimResult.Succeeded && customRoleClaimResult.Succeeded)
                            {
                                Console.WriteLine($"✓ Added role claims to user '{user.Email}' (ID: {user.Id}) with role '{user.Role}'");
                            }
                            else
                            {
                                var errors = roleClaimResult.Errors.Concat(customRoleClaimResult.Errors);
                                Console.WriteLine($"✗ Failed to add claims to user '{user.Email}': {string.Join(", ", errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"- User '{user.Email}' already has role claims.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error processing user '{user.Email}': {ex.Message}");
                    }
                }

                // Verify claims were saved
                var totalUsersWithClaims = 0;
                foreach (var user in ownerUsersToProcess)
                {
                    var claims = await _userManager.GetClaimsAsync(user);
                    if (claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "Role") && (c.Value == "Owner")))
                    {
                        totalUsersWithClaims++;
                    }
                }
                Console.WriteLine($"Claims verification: {totalUsersWithClaims}/{ownerUsersToProcess.Count} Owner users have role claims.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in EnsureOwnerClaimsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async Task SeedDemoDataAsync()
        {
            // Check if demo data already exists
            if (await _context.DonorProfile.AnyAsync())
            {
                Console.WriteLine("Demo data already exists. Skipping seed.");
                return;
            }

            var defaultPassword = "Demo@123"; // Default password for all demo users

            // Demo donor users
            var demoDonors = new (string FirstName, string LastName, string Email, int BloodTypeId, int LocationId, string Gender, DateOnly DateOfBirth, bool IsAvailable, bool IsHealthy, DateTime? LastDonation)[]
            {
                ("Ahmad", "Khoury", "ahmad.khoury@example.com", 1, 1, "Male", new DateOnly(1990, 5, 15), true, true, DateTime.UtcNow.AddMonths(-2)),
                ("Sarah", "Fadel", "sarah.fadel@example.com", 2, 2, "Female", new DateOnly(1995, 8, 22), true, true, DateTime.UtcNow.AddMonths(-1)),
                ("Mohammad", "Saad", "mohammad.saad@example.com", 7, 11, "Male", new DateOnly(1988, 3, 10), true, true, DateTime.UtcNow.AddDays(-45)),
                ("Layla", "Mansour", "layla.mansour@example.com", 3, 5, "Female", new DateOnly(1992, 11, 30), false, true, DateTime.UtcNow.AddDays(-20)),
                ("Omar", "Haddad", "omar.haddad@example.com", 8, 1, "Male", new DateOnly(1993, 7, 5), true, true, null),
                ("Rania", "Tannous", "rania.tannous@example.com", 5, 8, "Female", new DateOnly(1991, 2, 18), true, true, DateTime.UtcNow.AddMonths(-3)),
                ("Karim", "Nasser", "karim.nasser@example.com", 4, 18, "Male", new DateOnly(1989, 9, 25), true, false, DateTime.UtcNow.AddMonths(-6)),
                ("Nour", "Beydoun", "nour.beydoun@example.com", 6, 3, "Female", new DateOnly(1994, 4, 12), true, true, DateTime.UtcNow.AddDays(-60)),
                ("Hassan", "Younes", "hassan.younes@example.com", 1, 6, "Male", new DateOnly(1996, 12, 8), true, true, DateTime.UtcNow.AddDays(-30)),
                ("Maya", "Sleiman", "maya.sleiman@example.com", 7, 4, "Female", new DateOnly(1997, 6, 20), false, true, DateTime.UtcNow.AddDays(-15))
            };

            Console.WriteLine("Creating demo donor users...");

            foreach (var donorData in demoDonors)
            {
                var donor = await _userManager.FindByEmailAsync(donorData.Email);
                
                if (donor == null)
                {
                    donor = new Users
                    {
                        FirstName = donorData.FirstName,
                        LastName = donorData.LastName,
                        Email = donorData.Email,
                        UserName = donorData.Email,
                        EmailConfirmed = true,
                        Role = "Donor",
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(donor, defaultPassword);
                    
                    if (result.Succeeded)
                    {
                        // Create donor profile
                        var donorProfile = new DonorProfile
                        {
                            DonorId = donor.Id,
                            BloodTypeId = donorData.BloodTypeId,
                            LocationId = donorData.LocationId,
                            Gender = donorData.Gender,
                            DateOfBirth = donorData.DateOfBirth,
                            IsAvailable = donorData.IsAvailable,
                            IsHealthyForDonation = donorData.IsHealthy,
                            IsIdentityHidden = false,
                            LastDonationDate = donorData.LastDonation,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.DonorProfile.Add(donorProfile);
                        Console.WriteLine($"Created donor: {donorData.FirstName} {donorData.LastName} ({donorData.Email})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create donor {donorData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Create demo donor requests
            Console.WriteLine("Creating demo donor requests...");

            var demoRequests = new (string PatientName, int BloodTypeId, int LocationId, string UrgencyLevel, string ContactNumber, string HospitalName, string Notes, string Status)[]
            {
                ("Ali Khoury", 1, 1, "Critical", "+961-3-123456", "American University of Beirut Medical Center", "Emergency surgery required", "Pending"),
                ("Fatima Moussa", 2, 2, "High", "+961-1-789012", "Hotel Dieu de France", "Blood transfusion needed", "Pending"),
                ("Youssef Makki", 7, 11, "Critical", "+961-6-345678", "Islamic Hospital", "Accident victim - urgent", "Approved"),
                ("Mariam Fawaz", 5, 8, "Normal", "+961-8-901234", "Zahle Government Hospital", "Scheduled surgery", "Pending"),
                ("Bilal Hamdan", 8, 1, "Critical", "+961-3-567890", "Rafik Hariri University Hospital", "Rare blood type needed urgently", "Pending"),
                ("Lina Jaber", 3, 5, "High", "+961-9-234567", "Notre Dame des Secours Hospital", "Chemotherapy patient", "Approved"),
                ("Tarek Salloum", 4, 18, "Normal", "+961-7-890123", "Saida Government Hospital", "Regular transfusion", "Completed"),
                ("Dina Harb", 6, 3, "Low", "+961-4-456789", "Aley Hospital", "Pre-surgery preparation", "Pending")
            };

            foreach (var requestData in demoRequests)
            {
                var request = new DonorRequest
                {
                    PatientName = requestData.PatientName,
                    BloodTypeId = requestData.BloodTypeId,
                    LocationId = requestData.LocationId,
                    UrgencyLevel = requestData.UrgencyLevel,
                    ContactNumber = requestData.ContactNumber,
                    HospitalName = requestData.HospitalName,
                    AdditionalNotes = requestData.Notes,
                    Status = requestData.Status,
                    CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 30)),
                    CompletedAt = requestData.Status == "Completed" ? DateTime.UtcNow.AddDays(-new Random().Next(1, 10)) : null
                };

                _context.DonorRequests.Add(request);
                Console.WriteLine($"Created request: {requestData.PatientName} - {requestData.UrgencyLevel} urgency");
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Demo data seeding completed successfully!");
        }
    }
}

