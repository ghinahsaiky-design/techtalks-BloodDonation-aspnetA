using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models   // <-- use the same namespace as your other models
{
    public class ContactViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Phone { get; set; }

        [Required]
        public string Message { get; set; }
    }
}
