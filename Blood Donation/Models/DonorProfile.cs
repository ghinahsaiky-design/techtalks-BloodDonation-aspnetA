using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    public class DonorProfile
    {
        [Key]
        [ForeignKey("UserId")] // links to Users.UserId
        public int DonorId { get; set; }

        public Users User { get; set; } // navigation property to User

        [Required]
        public int BloodTypeId { get; set; }

        [ForeignKey("BloodTypeId")]
        public BloodType BloodType { get; set; } // navigation property

        [Required]
        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public Location Location { get; set; } // navigation property

        [Required]
        public bool IsHealthyForDonation { get; set; }

        [Required]
        public bool IsIdentityHidden { get; set; }

        [Required]
        public bool IsAvailable { get; set; }

        public DateTime? LastDonationDate { get; set; } // optional

        [Required]
        public int Age { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } // "Male" or "Female"
    }
}
