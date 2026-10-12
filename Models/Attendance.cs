using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolEventManagement.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RegistrationId { get; set; }
        [ForeignKey("RegistrationId")]
        public Registration Registration { get; set; }

        public DateTime CheckedInAt { get; set; } = DateTime.Now;

        [Required]
        public string VerificationMethod { get; set; } 

        public string CheckedInByAdminId { get; set; }
    }
}