using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolEventManagement.Models
{
    public class Evaluation
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int OverallRating { get; set; }

        [Range(1, 5)]
        public int ContentQuality { get; set; }

        [Range(1, 5)]
        public int SpeakerRating { get; set; }

        [Range(1, 5)]
        public int VenueRating { get; set; }

        [MaxLength(1000)]
        public string? KeyTakeaways { get; set; }

        [MaxLength(1000)]
        public string? Suggestions { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}