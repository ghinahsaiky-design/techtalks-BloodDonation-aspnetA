using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class AddNewHospitalViewModel
    {
        [Required]
        [Display(Name = "Hospital Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "License")]
        public string License { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Display(Name = "Zip")]
        public string Zip { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}