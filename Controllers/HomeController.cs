using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventManagement.Data;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.OrderBy(e => e.EventDate).ToListAsync();

            ViewBag.Events = events;
            ViewBag.RegisteredCount = await _context.Registrations.CountAsync();
            ViewBag.CheckedInCount = await _context.Attendances.CountAsync();

            // Evaluations
            int evalCount = await _context.Evaluations.CountAsync();
            double avgRating = evalCount > 0
                ? await _context.Evaluations.AverageAsync(e => (double)e.OverallRating)
                : 0.0;
            ViewBag.EvaluationCount = evalCount;
            ViewBag.AvgRating = avgRating;

            // Registered vs checked-in per event (for the bar chart)
            ViewBag.EventStats = await _context.Events
                .OrderBy(e => e.EventDate)
                .Select(e => new EventAttendanceStat
                {
                    EventId = e.Id,
                    Title = e.Title,
                    EventDate = e.EventDate,
                    Registered = e.Registrations.Count(),
                    CheckedIn = e.Registrations.Count(r => r.Attendance != null)
                })
                .ToListAsync();

            // Latest check-ins (activity feed)
            ViewBag.RecentCheckIns = await _context.Attendances
                .OrderByDescending(a => a.CheckedInAt)
                .Take(5)
                .Select(a => new RecentCheckIn
                {
                    Student = string.IsNullOrEmpty(a.Registration.StudentName) ? a.Registration.UserId : a.Registration.StudentName,
                    EventTitle = a.Registration.Event.Title,
                    Time = a.CheckedInAt,
                    Method = a.VerificationMethod
                })
                .ToListAsync();

            return View(events);
        }
    }
}