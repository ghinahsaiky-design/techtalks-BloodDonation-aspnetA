using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class Locations
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } // e.g., city, region, or area
    }
}
