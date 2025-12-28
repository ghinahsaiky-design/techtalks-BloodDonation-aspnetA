using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    public class DonorConfirmation
    {
        [Key]
        public int ConfirmationId { get; set; }

        [Required]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public DonorRequest Request { get; set; }

        [Required]
        public int DonorId { get; set; }

        [ForeignKey("DonorId")]
        public DonorProfile Donor { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Declined, Pending

        [MaxLength(500)]
        public string? Message { get; set; } // Donor's reply message

        [Required]
        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ContactedAt { get; set; } // When admin contacted the donor

        [MaxLength(200)]
        public string? AdminNotes { get; set; }
    }
}

