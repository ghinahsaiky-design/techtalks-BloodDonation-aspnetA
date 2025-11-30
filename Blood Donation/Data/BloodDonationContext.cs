using Microsoft.EntityFrameworkCore;
using BloodDonation.Models;
using System.Collections.Generic;

namespace BloodDonation.Data
{
    public class BloodDonationContext : DbContext
    {
        public BloodDonationContext(DbContextOptions<BloodDonationContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<DonorProfile> DonorProfiles { get; set; }
        public DbSet<BloodType> BloodTypes { get; set; }
        public DbSet<Location> Locations { get; set; }
    }
}
