using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class HospitalRegistrationViewModel
    {
        [Required(ErrorMessage = "Hospital name is required")]
        [Display(Name = "Hospital Name")]
        public string HospitalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required")]
        [Display(Name = "Hospital ID / License Number")]
        public string License { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person name is required")]
        [Display(Name = "Contact Person (Full Name)")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Contact Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Street Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        [Display(Name = "Postal Code")]
        public string Zip { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Create Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must agree to the terms and conditions")]
        public bool AgreeToTerms { get; set; }
    }

    public class CreateBloodRequestViewModel
    {
        [Required(ErrorMessage = "Blood type is required")]
        [Display(Name = "Blood Type")]
        public int BloodTypeId { get; set; }

        [Display(Name = "Component")]
        public string Component { get; set; } = "rbc"; // whole, rbc, platelets, plasma, cryo

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 50, ErrorMessage = "Quantity must be between 1 and 50 units")]
        [Display(Name = "Quantity (Units)")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Urgency level is required")]
        [Display(Name = "Urgency Level")]
        public string UrgencyLevel { get; set; } = "routine"; // routine, urgent, critical

        [Required(ErrorMessage = "Date required is needed")]
        [DataType(DataType.Date)]
        [Display(Name = "Date Required")]
        public DateTime DateRequired { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Time required is needed")]
        [DataType(DataType.Time)]
        [Display(Name = "Time Required")]
        public TimeSpan TimeRequired { get; set; }

        [Display(Name = "Delivery Location")]
        public string? DeliveryLocation { get; set; }

        [Display(Name = "Patient MRN / ID")]
        public string? PatientMRN { get; set; }

        [Display(Name = "Diagnosis / Clinical Indication")]
        public string? Diagnosis { get; set; }

        [Display(Name = "Additional Notes")]
        [MaxLength(500)]
        public string? AdditionalNotes { get; set; }

        // For dropdowns
        public List<BloodTypes> BloodTypes { get; set; } = new List<BloodTypes>();
    }

    public class HospitalDashboardViewModel
    {
        public Hospital? Hospital { get; set; }
        
        public int TotalRequested { get; set; }
        public int TotalReceived { get; set; }
        public int PendingRequests { get; set; }
        public double SuccessRate { get; set; }
        
        public List<DonorRequest> RecentRequests { get; set; } = new List<DonorRequest>();
        public Dictionary<string, int> BloodTypeDemand { get; set; } = new Dictionary<string, int>();
        
        // For charts
        public List<DailyRequestCount> RequestTrends { get; set; } = new List<DailyRequestCount>();
        public Dictionary<string, double> BloodTypePercentages { get; set; } = new Dictionary<string, double>();
        public int Last30DaysTotal { get; set; }
        public double Last30DaysGrowth { get; set; }
    }

    public class DailyRequestCount
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class HospitalRequestsViewModel
    {
        public List<DonorRequest> Requests { get; set; } = new List<DonorRequest>();
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
    }

    public class MatchedDonorsViewModel
    {
        public DonorRequest Request { get; set; } = null!;
        public List<MatchedDonorViewModel> MatchedDonors { get; set; } = new List<MatchedDonorViewModel>();
    }

    public class MatchedDonorViewModel
    {
        public int ConfirmationId { get; set; }
        public int DonorId { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ContactedAt { get; set; }
        public string? Message { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsHealthy { get; set; }
    }
}

