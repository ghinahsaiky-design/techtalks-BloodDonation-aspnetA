namespace BloodDonation.Models
{
    public class MonthlyPerformanceViewModel
    {
        public DateTime MonthDate { get; set; }   // ✅ for ordering
        public string Month { get; set; }
        public int Requested { get; set; }
        public int Fulfilled { get; set; }
        public double Efficiency { get; set; }
    }

}
