namespace BloodDonation.Models
{
    public class DashboardViewModel
    {
        public int TotalDonors { get; set; }
        public int SuccessfulDonations { get; set; }
        public int ActiveRequests { get; set; }
        public int BloodStockPercentage { get; set; }
        public int TotalHealthyDonors { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public List<DonationTrendViewModel> DonationTrends { get; set; } = new();
        public List<BloodTypeDistributionViewModel> BloodTypeDistribution { get; set; } = new();
        public List<RecentDonorViewModel> RecentDonors { get; set; } = new();
    }

    public class DonationTrendViewModel
    {
        public string Month { get; set; } = string.Empty;
        public string MonthShort { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class BloodTypeDistributionViewModel
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class RecentDonorViewModel
    {
        public string DonorName { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool HasDonated { get; set; }
    }
}

