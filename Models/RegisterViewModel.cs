using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")] 
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Blood Type")]
        public string BloodType { get; set; } = string.Empty;

        [Display(Name = "I confirm I am eligible to donate.")]
        public bool IsHealthyForDonation { get; set; }

        [Display(Name = "Hide my identity from recipients.")]
        public bool IsIdentityHidden { get; set; }
    }
}
