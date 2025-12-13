using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class RegisterViewModel
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

        // -----------------------------
        // FIXED VALUES TO MATCH DATABASE
        // -----------------------------
        [Required]
        [Display(Name = "Location")]
        public int LocationId { get; set; }   // <-- MUST BE INT

        [Required]
        [Display(Name = "Blood Type")]
        public int BloodTypeId { get; set; }   // <-- MUST BE INT


        [Display(Name = "I confirm I am eligible to donate.")]
        public bool IsHealthyForDonation { get; set; }

        [Display(Name = "Hide my identity from recipients.")]
        public bool IsIdentityHidden { get; set; }

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;


        // Lists used for dropdowns
        public List<Locations> Locations { get; set; } = new();
        public List<BloodTypes> BloodTypes { get; set; } = new();
    }
}
