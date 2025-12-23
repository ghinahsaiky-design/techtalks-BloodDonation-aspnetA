using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class Users:IdentityUser<int>
    {


        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        //[Required]
        //[EmailAddress]
      //  [MaxLength(100)]
       // public string Email { get; set; } 

     

        //[MaxLength(20)]
        //public string? Phone { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // "Donor", "Admin", "Owner", "Hospital"

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 
