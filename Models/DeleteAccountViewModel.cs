using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class DeleteAccountViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
