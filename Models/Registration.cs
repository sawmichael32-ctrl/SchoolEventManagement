using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolEventManagement.Models
{
    public class Registration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; }

        // Student ID number (used as the identity key for check-in and evaluation)
        [Required]
        [Display(Name = "Student ID")]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Student Name")]
        public string StudentName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "School Level")]
        public string SchoolLevel { get; set; } = "College";

        [StringLength(20)]
        [Display(Name = "Grade / Year Level")]
        public string GradeLevel { get; set; } = string.Empty;

        // Kept as Course for compatibility with existing teacher course reports.
        // For SHS this stores the strand/program (e.g. ICT, STEM); for JHS it can be blank.
        [Required]
        [StringLength(100)]
        [Display(Name = "Course / Program / Strand")]
        public string Course { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Section { get; set; } = string.Empty;

        [Required]
        public string QRCodeToken { get; set; } = Guid.NewGuid().ToString(); 

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

       
        public Attendance Attendance { get; set; }
    }
}