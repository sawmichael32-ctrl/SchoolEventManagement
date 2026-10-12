using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventManagement.Data;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Controllers
{
    public class EvaluationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EvaluationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        //  EVALUATION PORTAL ACCESS (STANDALONE TAB)
        // ==========================================

        // GET: Evaluations/Access
        public async Task<IActionResult> Access()
        {
            ViewBag.Events = await _context.Events.ToListAsync();
            return View();
        }

        // POST: Evaluations/VerifyAccess
        [HttpPost]
        public async Task<IActionResult> VerifyAccess(int eventId, string studentId)
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Access));
            }

            // FIXED: Block evaluation access only after EndDateTime has passed
            if (@event.EndDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = $"Evaluations are closed. The event '{@event.Title}' has already ended.";
                return RedirectToAction(nameof(Access));
            }

            if (string.IsNullOrWhiteSpace(studentId))
            {
                TempData["ErrorMessage"] = "Please enter your Name/Student ID or scan your QR Code.";
                return RedirectToAction(nameof(Access));
            }

            var cleanInput = studentId.Trim();

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId &&
                    (r.UserId.ToLower() == cleanInput.ToLower() || r.QRCodeToken == cleanInput));

            if (registration == null)
            {
                TempData["ErrorMessage"] = $"No registration record found for '{cleanInput}' in this event.";
                return RedirectToAction(nameof(Access));
            }

            var hasAttended = await _context.Attendances
                .AnyAsync(a => a.RegistrationId == registration.Id);

            if (!hasAttended)
            {
                TempData["ErrorMessage"] = $"Attendance not verified for '{registration.UserId}'. You must check in at the venue before evaluating.";
                return RedirectToAction(nameof(Access));
            }

            var existingEvaluation = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.UserId.ToLower() == registration.UserId.ToLower());

            if (existingEvaluation != null)
            {
                TempData["ErrorMessage"] = $"An evaluation has already been submitted for '{registration.UserId}'. Thank you!";
                return RedirectToAction(nameof(Access));
            }

            return RedirectToAction("Create", new { eventId = eventId, studentId = registration.UserId });
        }

        // ==========================================
        //  EVALUATION FORM SUBMISSION
        // ==========================================

        // GET: Evaluations/Create?eventId=1&studentId=mj
        public async Task<IActionResult> Create(int eventId, string studentId = "")
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null) return NotFound();

            // FIXED: Check EndDateTime instead of EventDate
            if (@event.EndDateTime < DateTime.Now)
            {
                TempData["NoticeMessage"] = $"Evaluations are closed. The event '{@event.Title}' has already ended.";
                return RedirectToAction("Index", "Events");
            }

            if (string.IsNullOrWhiteSpace(studentId))
            {
                TempData["NoticeMessage"] = "Please log in through the Evaluation Portal first.";
                return RedirectToAction(nameof(Access));
            }

            var cleanStudentId = studentId.Trim();

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId.ToLower() == cleanStudentId.ToLower());

            if (registration == null)
            {
                TempData["NoticeMessage"] = "You must be registered for this event to evaluate it.";
                return RedirectToAction("Index", "Events");
            }

            var hasAttended = await _context.Attendances
                .AnyAsync(a => a.RegistrationId == registration.Id);

            if (!hasAttended)
            {
                TempData["NoticeMessage"] = "You can only submit an evaluation after attending (checking in) the event!";
                return RedirectToAction("Index", "Events");
            }

            var existingEvaluation = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.UserId.ToLower() == cleanStudentId.ToLower());

            if (existingEvaluation != null)
            {
                TempData["NoticeMessage"] = "You have already submitted an evaluation for this event.";
                return RedirectToAction("Index", "Events");
            }

            ViewBag.EventTitle = @event.Title;
            ViewBag.EventDate = @event.EventDate;
            ViewBag.Venue = @event.Venue;

            var evaluation = new Evaluation
            {
                EventId = eventId,
                UserId = registration.UserId
            };

            return View(evaluation);
        }

        // POST: Evaluations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,UserId,OverallRating,ContentQuality,SpeakerRating,VenueRating,KeyTakeaways,Suggestions")] Evaluation evaluation)
        {
            ModelState.Remove("Event");

            var @event = await _context.Events.FindAsync(evaluation.EventId);
            // FIXED: Check EndDateTime instead of EventDate
            if (@event == null || @event.EndDateTime < DateTime.Now)
            {
                TempData["NoticeMessage"] = "Evaluations are closed because this event has already ended.";
                return RedirectToAction("Index", "Events");
            }

            if (string.IsNullOrWhiteSpace(evaluation.UserId))
            {
                TempData["NoticeMessage"] = "Invalid student identity. Please try again via the Evaluation Portal.";
                return RedirectToAction(nameof(Access));
            }

            var cleanUserId = evaluation.UserId.Trim();

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == evaluation.EventId && r.UserId.ToLower() == cleanUserId.ToLower());

            if (registration == null || !await _context.Attendances.AnyAsync(a => a.RegistrationId == registration.Id))
            {
                TempData["NoticeMessage"] = "Only verified attendees can submit an evaluation for this event.";
                return RedirectToAction("Index", "Events");
            }

            if (ModelState.IsValid)
            {
                evaluation.UserId = registration.UserId;
                evaluation.SubmittedAt = DateTime.Now;

                _context.Add(evaluation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you! Your event feedback has been submitted successfully.";
                return RedirectToAction("Index", "Events");
            }

            ViewBag.EventTitle = @event.Title;
            ViewBag.EventDate = @event.EventDate;
            ViewBag.Venue = @event.Venue;

            return View(evaluation);
        }

        // ==========================================
        //  EVALUATION SUMMARY & ANALYTICS REPORT
        // ==========================================

        // GET: Evaluations/Summary?eventId=1
        public async Task<IActionResult> Summary(int eventId)
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null) return NotFound();

            var evaluations = await _context.Evaluations
                .Where(e => e.EventId == eventId)
                .ToListAsync();

            ViewBag.EventTitle = @event.Title;
            ViewBag.TotalEvaluations = evaluations.Count;

            if (evaluations.Any())
            {
                ViewBag.AvgOverall = evaluations.Average(e => e.OverallRating);
                ViewBag.AvgContent = evaluations.Average(e => e.ContentQuality);
                ViewBag.AvgSpeaker = evaluations.Average(e => e.SpeakerRating);
                ViewBag.AvgVenue = evaluations.Average(e => e.VenueRating);
            }
            else
            {
                ViewBag.AvgOverall = 0;
                ViewBag.AvgContent = 0;
                ViewBag.AvgSpeaker = 0;
                ViewBag.AvgVenue = 0;
            }

            return View(evaluations);
        }
    }
}