using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class Users
    {
        [Key] // Primary Key
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; } 

        [Required]
        public string Password { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // "Donor", "Admin", "Owner"
    }
}
