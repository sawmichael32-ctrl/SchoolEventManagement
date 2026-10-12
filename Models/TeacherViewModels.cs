using System.ComponentModel.DataAnnotations;

namespace SchoolEventManagement.Models
{
    // One student's registration (and check-in, if any) for an event
    public class CourseAttendanceRow
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public bool Attended { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string? Method { get; set; }
    }

    public class CourseAttendanceViewModel
    {
        public string? Search { get; set; }
        public List<string> MyCourses { get; set; } = new();
        public string? SelectedCourse { get; set; }
        public List<CourseAttendanceRow> Rows { get; set; } = new();

        public int TotalRegistered => Rows.Count;
        public int TotalAttended => Rows.Count(r => r.Attended);
        public int TotalNotCheckedIn => TotalRegistered - TotalAttended;
    }

    // Used by the admin screen that creates / edits teacher accounts
    public class TeacherFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Courses handled")]
        public string? CoursesText { get; set; }
    }
}
