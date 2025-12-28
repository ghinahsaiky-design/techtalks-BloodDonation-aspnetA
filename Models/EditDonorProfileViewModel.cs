using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

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
        public string? ProfileImagePath { get; set; }

        // DONOR INFO
        [Required]
        public int BloodTypeId { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = "";

        public string Phone { get; set; } = "";

        [Required]
        public bool IsHealthyForDonation { get; set; }

        [Required]
        public bool IsIdentityHidden { get; set; }

        [Required]
        public bool IsAvailable { get; set; }

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [MaxLength(10)]
        public string Gender { get; set; } = "";
    }
}
