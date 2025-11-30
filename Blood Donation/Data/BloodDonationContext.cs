using Microsoft.EntityFrameworkCore;
using BloodDonation.Models;
using System.Collections.Generic;

namespace BloodDonation.Data
{
    public class BloodDonationContext : DbContext
    {
        public BloodDonationContext(DbContextOptions<BloodDonationContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<DonorProfile> DonorProfile { get; set; }
        public DbSet<BloodTypes> BloodTypes { get; set; }
        public DbSet<Locations> Locations { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }

    }
}
