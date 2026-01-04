using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    public class Hospital
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string License { get; set; } = string.Empty;

        // Link to Users table (User with Role = "Hospital")
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;

        [MaxLength(400)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Zip { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Deleted

        public virtual ICollection<HospitalStaff> HospitalStaff { get; set; } = new List<HospitalStaff>();
    }
}