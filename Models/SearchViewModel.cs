using Microsoft.AspNetCore.Mvc.Rendering;

namespace BloodDonation.Models
{
    public class SearchViewModel
    {
        // Inputs from the form
        public int? SelectedLocationId { get; set; }
        public int? SelectedBloodTypeId { get; set; }

        // Dropdown data
        public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> BloodTypes { get; set; } = new List<SelectListItem>();

        // Results to display
        public List<SearchResultViewModel> Results { get; set; } = new List<SearchResultViewModel>();
    }
}
