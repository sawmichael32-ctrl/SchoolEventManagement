using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolEventManagement.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        // Hashed with ASP.NET Core PasswordHasher; the plain password is never stored
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<TeacherCourse> Courses { get; set; } = new List<TeacherCourse>();
    }

    // A course handled by a teacher. CourseName must match the "Course" students type when registering.
    public class TeacherCourse
    {
        [Key]
        public int Id { get; set; }

        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public Teacher? Teacher { get; set; }

        [Required]
        [StringLength(100)]
        public string CourseName { get; set; } = string.Empty;
    }
}
