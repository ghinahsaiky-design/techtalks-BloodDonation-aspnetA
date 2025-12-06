using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class Locations
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Districts { get; set; } 
    } 
}
