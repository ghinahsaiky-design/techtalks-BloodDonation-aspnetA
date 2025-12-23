using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class AddNewHospitalViewModel
    {
        [Required(ErrorMessage = "Hospital name is required")]
        [Display(Name = "Hospital Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "License")]
        public string License { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required for new accounts")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Zip code is required")]
        [Display(Name = "Zip")]
        public string Zip { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";
    }
}