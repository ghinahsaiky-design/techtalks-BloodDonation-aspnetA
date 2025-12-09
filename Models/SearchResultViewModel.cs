namespace BloodDonation.Models
{
    public class SearchResultViewModel
    {
        public string DonorName { get; set; }
        public string BloodType { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsAvailable { get; set; }
    }
}
