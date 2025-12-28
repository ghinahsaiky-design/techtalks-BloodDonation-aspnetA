namespace BloodDonation.Models
{
    public class ProfileViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? ProfileImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
        public int TotalActions { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string ActionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ActionType Type { get; set; }
        public DateTime PerformedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}

