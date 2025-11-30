using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class BloodType
    {
        [Key]
        public int BloodTypeId { get; set; }

        [Required]
        [MaxLength(3)]
        public string Type { get; set; } 
    }
}
