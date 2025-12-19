using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    public class DonorRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(200)]
        public string PatientName { get; set; } = "";

        [Required]
        public int BloodTypeId { get; set; }

        [ForeignKey("BloodTypeId")]
        public BloodTypes BloodType { get; set; }

        [Required]
        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public Locations Location { get; set; }

        [Required]
        [MaxLength(50)]
        public string UrgencyLevel { get; set; } = "Normal"; // Critical, High, Normal, Low

        [Required]
        [MaxLength(20)]
        public string ContactNumber { get; set; } = "";

        [MaxLength(100)]
        public string? HospitalName { get; set; }

        [MaxLength(500)]
        public string? AdditionalNotes { get; set; }

        public int? RequestedByUserId { get; set; }

        [ForeignKey("RequestedByUserId")]
        public Users? RequestedByUser { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Completed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }
}

