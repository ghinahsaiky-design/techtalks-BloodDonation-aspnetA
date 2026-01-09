namespace BloodDonation.Models
{
    public class MonthlyTrendViewModel
    {
        public DateTime MonthDate { get; set; }   // ✅ for ordering
        public string Month { get; set; }         // display text
        public int Requested { get; set; }
        public int Completed { get; set; }
    }

}
