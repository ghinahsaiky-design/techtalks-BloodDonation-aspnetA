using BloodDonation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BloodDonation.Data
{
    public class BloodDonationContext
    : IdentityDbContext<Users, IdentityRole<int>, int>

    {
        public BloodDonationContext(DbContextOptions<BloodDonationContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<DonorProfile> DonorProfile { get; set; }
        public DbSet<BloodTypes> BloodTypes { get; set; }
        public DbSet<Locations> Locations { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<TrackedAction> Actions { get; set; }
        public DbSet<DonorRequest> DonorRequests { get; set; }
        public DbSet<DonorConfirmation> DonorConfirmations { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<HospitalNotification> HospitalNotifications { get; set; }
        public DbSet<HospitalStaff> HospitalStaff { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BloodTypes>().HasData(
                new BloodTypes { BloodTypeId = 1, Type = "A+" },
                new BloodTypes { BloodTypeId = 2, Type = "A-" },
                new BloodTypes { BloodTypeId = 3, Type = "B+" },
                new BloodTypes { BloodTypeId = 4, Type = "B-" },
                new BloodTypes { BloodTypeId = 5, Type = "AB+" },
                new BloodTypes { BloodTypeId = 6, Type = "AB-" },
                new BloodTypes { BloodTypeId = 7, Type = "O+" },
                new BloodTypes { BloodTypeId = 8, Type = "O-" }
            );

            // Locations (Districts of Lebanon)
            modelBuilder.Entity<Locations>().HasData(
                new Locations { LocationId = 1, Districts = "Beirut" },
                new Locations { LocationId = 2, Districts = "Baabda" },
                new Locations { LocationId = 3, Districts = "Aley" },
                new Locations { LocationId = 4, Districts = "Chouf" },
                new Locations { LocationId = 5, Districts = "Keserwan" },
                new Locations { LocationId = 6, Districts = "Matn" },
                new Locations { LocationId = 7, Districts = "Jbeil" },
                new Locations { LocationId = 8, Districts = "Zahle" },
                new Locations { LocationId = 9, Districts = "Baalbek" },
                new Locations { LocationId = 10, Districts = "Hermel" },
                new Locations { LocationId = 11, Districts = "Tripoli" },
                new Locations { LocationId = 12, Districts = "Miniyeh-Danniyeh" },
                new Locations { LocationId = 13, Districts = "Zgharta" },
                new Locations { LocationId = 14, Districts = "Koura" },
                new Locations { LocationId = 15, Districts = "Batroun" },
                new Locations { LocationId = 16, Districts = "Bcharre" },
                new Locations { LocationId = 17, Districts = "Akkar" },
                new Locations { LocationId = 18, Districts = "Saida" },
                new Locations { LocationId = 19, Districts = "Tyre" },
                new Locations { LocationId = 20, Districts = "Jezzine" },
                new Locations { LocationId = 21, Districts = "Nabatieh" },
                new Locations { LocationId = 22, Districts = "Bint Jbeil" },
                new Locations { LocationId = 23, Districts = "Marjeyoun" },
                new Locations { LocationId = 24, Districts = "Hasbaya" },
                new Locations { LocationId = 25, Districts = "Rachaya"},
                new Locations { LocationId = 26, Districts = "West Beqaa"}
            );

            // =========================
            // RELATIONSHIPS
            // =========================

            // DonorConfirmation -> DonorRequest  (NO CASCADE)
            modelBuilder.Entity<DonorConfirmation>()
                .HasOne(dc => dc.Request)
                .WithMany()                             // no collection on DonorRequest
                .HasForeignKey(dc => dc.RequestId)
                .OnDelete(DeleteBehavior.NoAction);

            // DonorConfirmation -> DonorProfile (Cascade is fine)
            modelBuilder.Entity<DonorConfirmation>()
                .HasOne(dc => dc.Donor)
                .WithMany()                             // or .WithMany(d => d.Confirmations)
                .HasForeignKey(dc => dc.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            // HospitalNotification -> DonorRequest (NO CASCADE)
            modelBuilder.Entity<HospitalNotification>()
                .HasOne(hn => hn.Request)
                .WithMany()
                .HasForeignKey(hn => hn.RequestId)
                .OnDelete(DeleteBehavior.NoAction);

            // HospitalStaff -> Hospital  (keep cascade)
            modelBuilder.Entity<HospitalStaff>()
                .HasOne(hs => hs.Hospital)
                .WithMany()                             // or .WithMany(h => h.Staff)
                .HasForeignKey(hs => hs.HospitalId)
                .OnDelete(DeleteBehavior.Cascade);

            // HospitalStaff -> Users (NO CASCADE to avoid multiple cascade paths)
            modelBuilder.Entity<HospitalStaff>()
                .HasOne(hs => hs.User)
                .WithMany()                             // or .WithMany(u => u.HospitalStaff)
                .HasForeignKey(hs => hs.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }

    }

}

