using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    public class HospitalNotification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int HospitalUserId { get; set; } // User ID of the hospital

        [Required]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public DonorRequest Request { get; set; }

        [Required]
        [MaxLength(200)]
        public string Message { get; set; } = string.Empty; // e.g., "Request #22 status changed to Approved"

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Unread"; // Unread, Read

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }
    }
}

