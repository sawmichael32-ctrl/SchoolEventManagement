using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolEventManagement.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Venue { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Date & Time")]
        public DateTime EventDate { get; set; }

        [Required]
        [Display(Name = "End Date & Time")]
        public DateTime EndDateTime { get; set; }

        public string? CreatedById { get; set; }

        // Event audience. "All" is the default so existing events remain open to everyone.
        [Required]
        [StringLength(20)]
        [Display(Name = "Target School Level")]
        public string TargetSchoolLevel { get; set; } = "All";

        [StringLength(20)]
        [Display(Name = "Target Grade / Year Level")]
        public string? TargetGradeLevel { get; set; }

        [StringLength(100)]
        [Display(Name = "Target Program / Strand / Course")]
        public string? TargetProgram { get; set; }

        [StringLength(50)]
        [Display(Name = "Target Section")]
        public string? TargetSection { get; set; }

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}