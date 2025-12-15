using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // Optional: Add a RememberMe checkbox if you want that functionality later
        public bool RememberMe { get; set; }
    }
}
