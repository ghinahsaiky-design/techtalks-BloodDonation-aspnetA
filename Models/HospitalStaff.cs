using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    public class HospitalStaff
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HospitalId { get; set; }

        [ForeignKey("HospitalId")]
        public virtual Hospital Hospital { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;

        [MaxLength(50)]
        public string Role { get; set; } = "Staff"; // Admin, Coordinator, Staff

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Invited, Suspended

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? InvitedAt { get; set; }

        public int? InvitedByUserId { get; set; }
    }
}

