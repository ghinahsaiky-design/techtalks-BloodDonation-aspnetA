using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class OwnerDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDonations { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalDonors { get; set; }
        public int TotalHospitals { get; set; }
        public int TotalOwners { get; set; }
        
        public double UserGrowthPercentage { get; set; }
        public double DonationGrowthPercentage { get; set; }

        public List<int> DailyDonations { get; set; } = new List<int>();
        public List<int> DailyRegistrations { get; set; } = new List<int>();
        public List<int> DailyRequests { get; set; } = new List<int>();
        public List<string> DaysLabels { get; set; } = new List<string>();

        // For the chart
        public Dictionary<string, int> RoleDistribution { get; set; } = new Dictionary<string, int>();

        public List<TrackedAction> RecentActions { get; set; } = new List<TrackedAction>();
    }

    public class AdminListViewModel
    {
        public List<Users> Admins { get; set; } = new List<Users>();
        public int TotalAdmins { get; set; }
        public int ActiveAdmins { get; set; } // Assuming we can determine active status, otherwise just total
        public int PendingAdmins { get; set; }
        public int InactiveAdmins { get; set; }
    }

    public class AdminActionsViewModel
    {
        public List<TrackedAction> Actions { get; set; } = new List<TrackedAction>();
    }

    public class CreateAdminViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class EditAdminViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
