using System;

namespace BloodDonation.Models
{
    public class TrackedAction
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public ActionType Type { get; set; }
        public int PerformedByUserId { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
        public Users PerformedByUser { get; set; }
        public int ? TargetEntityId { get; set; }
        public int? TargetUserId { get; set; }


    }
    public enum ActionType
    {
        Create,
        Read,
        Update,
        Delete
    }
}
