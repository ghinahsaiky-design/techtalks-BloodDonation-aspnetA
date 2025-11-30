using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } // e.g., city, region, or area
    }
}
