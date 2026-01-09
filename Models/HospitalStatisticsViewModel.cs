namespace BloodDonation.Models.ViewModels
{
    public class HospitalStatisticsViewModel
    {
        // Top cards
        public double FulfillmentRate { get; set; }
        public double AverageCompletionTimeHours { get; set; }
        public int EngagedRequests { get; set; }
        public int UnmetRequests { get; set; }

        // Trend data
        public double FulfillmentTrend { get; set; }
        public double TimeTrendHours { get; set; }
        public double EngagedTrendPercent { get; set; }
        public int UnmetTrendCount { get; set; }

        // Analytics
        public SupplyDemandViewModel SupplyVsDemand { get; set; } = new();
        public CompletionRatioViewModel CompletionRatio { get; set; } = new();

        // Time series data
        public List<MonthlyTrendViewModel> MonthlyTrends { get; set; }
        public List<MonthlyPerformanceViewModel> MonthlyPerformance { get; set; }


        // Other collections
        public List<BloodTypeFulfillmentViewModel> BloodTypeFulfillment { get; set; } = new();
        public List<StatusBreakdownViewModel> StatusBreakdown { get; set; } = new();
    }


    public class SupplyDemandViewModel
    {
        public int Requested { get; set; }
        public int Completed { get; set; }
    }

    public class CompletionRatioViewModel
    {
        public int Completed { get; set; }
        public int NotCompleted { get; set; }
    }

    public class BloodTypeFulfillmentViewModel
    {
        public string BloodType { get; set; } = "";
        public int Demand { get; set; }
        public int Completed { get; set; }
    }

    public class StatusBreakdownViewModel
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
    }
}