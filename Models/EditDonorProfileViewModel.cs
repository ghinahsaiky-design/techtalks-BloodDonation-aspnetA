using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class EditDonorProfileViewModel
    {
        // USER INFO
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = "";

        // DONOR INFO
        [Required]
        public int BloodTypeId { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public bool IsHealthyForDonation { get; set; }

        [Required]
        public bool IsIdentityHidden { get; set; }

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } = "";
    }
}
