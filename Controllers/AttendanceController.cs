using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventManagement.Data;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attendance/Scanner
        public async Task<IActionResult> Scanner()
        {
            ViewBag.Events = await _context.Events.ToListAsync();
            return View();
        }

        // POST: Attendance/RecordAttendance (QR Scan)
        [HttpPost]
        public async Task<IActionResult> RecordAttendance(string qrToken, string verificationMethod = "QR Code")
        {
            if (string.IsNullOrEmpty(qrToken))
            {
                return Json(new { success = false, message = "Invalid QR Code scanned." });
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.QRCodeToken == qrToken);

            if (registration == null)
            {
                return Json(new { success = false, message = "Registration not found for this QR code." });
            }

            var eventItem = registration.Event;

            // CHECK IF EVENT HAS ALREADY PASSED (Previous days)
            if (eventItem != null && eventItem.EventDate.Date < DateTime.Today)
            {
                return Json(new { success = false, message = $"Check-in failed. The event '{eventItem.Title}' has already ended." });
            }

            // CHECK IF EVENT IS SCHEDULED FOR A FUTURE DAY
            if (eventItem != null && eventItem.EventDate.Date > DateTime.Today)
            {
                return Json(new { success = false, message = $"Check-in is not open yet. The event '{eventItem.Title}' is scheduled for {eventItem.EventDate:MMMM dd, yyyy}." });
            }

            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.RegistrationId == registration.Id);

            if (existingAttendance != null)
            {
                return Json(new
                {
                    success = false,
                    message = $"Already Checked-In at {existingAttendance.CheckedInAt:hh:mm tt}!",
                    student = string.IsNullOrWhiteSpace(registration.StudentName) ? registration.UserId : $"{registration.StudentName} ({registration.UserId})",
                    eventTitle = eventItem?.Title
                });
            }

            var attendance = new Attendance
            {
                RegistrationId = registration.Id,
                CheckedInAt = DateTime.Now,
                VerificationMethod = verificationMethod,
                CheckedInByAdminId = User.Identity?.Name ?? "Admin"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Attendance Recorded Successfully!",
                student = string.IsNullOrWhiteSpace(registration.StudentName) ? registration.UserId : $"{registration.StudentName} ({registration.UserId})",
                eventTitle = eventItem?.Title,
                time = attendance.CheckedInAt.ToString("hh:mm tt")
            });
        }

        // ==========================================
        //  MANUAL ATTENDANCE CHECK-IN LOGIC
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> RecordManualAttendance(int eventId, string studentUserId)
        {
            if (string.IsNullOrWhiteSpace(studentUserId))
            {
                return Json(new { success = false, message = "Please enter a valid Student ID." });
            }

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
            {
                return Json(new { success = false, message = "Event not found." });
            }

            // CHECK IF EVENT HAS ALREADY PASSED (Previous days)
            if (eventItem.EventDate.Date < DateTime.Today)
            {
                return Json(new { success = false, message = $"Check-in failed. The event '{eventItem.Title}' has already ended." });
            }

            // CHECK IF EVENT IS SCHEDULED FOR A FUTURE DAY
            if (eventItem.EventDate.Date > DateTime.Today)
            {
                return Json(new { success = false, message = $"Check-in is not open yet. The event '{eventItem.Title}' is scheduled for {eventItem.EventDate:MMMM dd, yyyy}." });
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId.ToLower() == studentUserId.Trim().ToLower());

            if (registration == null)
            {
                return Json(new { success = false, message = $"No registration record found for Student ID '{studentUserId}' in this event." });
            }

            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.RegistrationId == registration.Id);

            if (existingAttendance != null)
            {
                return Json(new
                {
                    success = false,
                    message = $"Student ID '{studentUserId}' was ALREADY checked in at {existingAttendance.CheckedInAt:hh:mm tt}!"
                });
            }

            var attendance = new Attendance
            {
                RegistrationId = registration.Id,
                CheckedInAt = DateTime.Now,
                VerificationMethod = "Manual",
                CheckedInByAdminId = User.Identity?.Name ?? "Admin"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Manual check-in successful for Student ID {registration.UserId}!",
                student = string.IsNullOrWhiteSpace(registration.StudentName) ? registration.UserId : $"{registration.StudentName} ({registration.UserId})",
                eventTitle = eventItem.Title,
                time = attendance.CheckedInAt.ToString("hh:mm tt")
            });
        }

        // ==========================================
        //  ATTENDANCE REPORT DASHBOARD
        // ==========================================

        // GET: Attendance/Report?eventId=1
        public async Task<IActionResult> Report(int eventId)
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null) return NotFound();

            var attendances = await _context.Attendances
                .Include(a => a.Registration)
                .Where(a => a.Registration.EventId == eventId)
                .OrderByDescending(a => a.CheckedInAt)
                .ToListAsync();

            var totalRegistrations = await _context.Registrations
                .CountAsync(r => r.EventId == eventId);

            ViewBag.EventTitle = @event.Title;
            ViewBag.EventDate = @event.EventDate;
            ViewBag.Venue = @event.Venue;
            ViewBag.TotalRegistered = totalRegistrations;
            ViewBag.TotalAttended = attendances.Count;

            return View(attendances);
        }
    }
}