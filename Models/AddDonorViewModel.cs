using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class AddDonorViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Blood Type")]
        public int BloodTypeId { get; set; }

        [Required]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Is Healthy for Donation")]
        public bool IsHealthyForDonation { get; set; } = true;

        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Hide Identity")]
        public bool IsIdentityHidden { get; set; } = false;

        [Display(Name = "Last Donation Date")]
        public DateTime? LastDonationDate { get; set; }

        // Lists for dropdowns
        public List<BloodTypes> BloodTypes { get; set; } = new();
        public List<Locations> Locations { get; set; } = new();
    }
}

