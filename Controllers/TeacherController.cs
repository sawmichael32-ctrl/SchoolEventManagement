using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventManagement.Data;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Teacher> _hasher = new();

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        //  TEACHER LOGIN / LOGOUT (session based, same approach as the admin login)
        // ==========================================

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("TeacherId") != null)
                return RedirectToAction(nameof(AttendanceReport));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var name = (username ?? string.Empty).Trim();
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Username == name);

            if (teacher == null ||
                _hasher.VerifyHashedPassword(teacher, teacher.PasswordHash, password ?? string.Empty) == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            HttpContext.Session.SetInt32("TeacherId", teacher.Id);
            HttpContext.Session.SetString("TeacherName", teacher.FullName);
            return RedirectToAction(nameof(AttendanceReport));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("TeacherId");
            HttpContext.Session.Remove("TeacherName");
            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        //  ATTENDANCE REPORT (teacher only)
        //  Search among the courses assigned to this teacher, then view the full report of one course.
        // ==========================================

        // GET: Teacher/AttendanceReport?search=bsit&course=BSIT
        public async Task<IActionResult> AttendanceReport(string? search, string? course)
        {
            var teacherId = HttpContext.Session.GetInt32("TeacherId");
            if (teacherId == null) return RedirectToAction(nameof(Login));

            ViewBag.TeacherName = HttpContext.Session.GetString("TeacherName");

            var assigned = await _context.TeacherCourses
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.CourseName)
                .OrderBy(n => n)
                .ToListAsync();

            var vm = new CourseAttendanceViewModel { Search = search };

            vm.MyCourses = string.IsNullOrWhiteSpace(search)
                ? assigned
                : assigned.Where(c => c.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(course))
            {
                // A teacher can only open courses assigned to them
                var selected = assigned.FirstOrDefault(c => string.Equals(c, course.Trim(), StringComparison.OrdinalIgnoreCase));
                if (selected == null) return NotFound();

                vm.SelectedCourse = selected;

                // Course is stored on each registration (typed by the student), so the report
                // collects every registration for this course across all events.
                vm.Rows = await _context.Registrations
                    .Where(r => r.Course == selected)
                    .OrderBy(r => r.Event.EventDate)
                    .ThenBy(r => r.StudentName)
                    .Select(r => new CourseAttendanceRow
                    {
                        EventId = r.EventId,
                        EventTitle = r.Event.Title,
                        EventDate = r.Event.EventDate,
                        StudentId = r.UserId,
                        StudentName = r.StudentName,
                        Section = r.Section,
                        Attended = r.Attendance != null,
                        CheckedInAt = r.Attendance != null ? r.Attendance.CheckedInAt : (DateTime?)null,
                        Method = r.Attendance != null ? r.Attendance.VerificationMethod : null
                    })
                    .ToListAsync();
            }

            return View(vm);
        }
    }
}
