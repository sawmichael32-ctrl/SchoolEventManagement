using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventManagement.Data;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin") == "true" || User.IsInRole("Admin");

            ViewBag.RegCounts = await _context.Registrations
                .GroupBy(r => r.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);

            ViewBag.TotalRegistrations = await _context.Registrations.CountAsync();

            var events = await _context.Events.OrderBy(e => e.EventDate).ToListAsync();
            return View(events);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Venue,EventDate,EndDateTime,TargetSchoolLevel,TargetGradeLevel,TargetProgram,TargetSection")] Event @event)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");

            ModelState.Remove("CreatedById");
            ModelState.Remove("Registrations");
            ModelState.Remove("Evaluations");
            ValidateEventAudience(@event);
            if (@event.EndDateTime <= @event.EventDate)
                ModelState.AddModelError(nameof(@event.EndDateTime), "End date and time must be after the start date and time.");

            if (ModelState.IsValid)
            {
                @event.CreatedById = User.Identity?.Name ?? "Admin";
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(@event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Venue,EventDate,EndDateTime,CreatedById,TargetSchoolLevel,TargetGradeLevel,TargetProgram,TargetSection")] Event @event)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");
            if (id != @event.Id) return NotFound();

            ModelState.Remove("Registrations");
            ModelState.Remove("Evaluations");
            ValidateEventAudience(@event);
            if (@event.EndDateTime <= @event.EventDate)
                ModelState.AddModelError(nameof(@event.EndDateTime), "End date and time must be after the start date and time.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.Id == @event.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin");

            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // ADMIN LOGIN / LOGOUT LOGIC
        // ==========================================

        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AdminLogin(string password)
        {
            if (password == "admin123")
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Error = "Invalid Admin Key / Password!";
            return View();
        }

        public IActionResult AdminLogout()
        {
            HttpContext.Session.Remove("IsAdmin");
            return RedirectToAction(nameof(Index));
        }

        private bool IsAdminUser()
        {
            return HttpContext.Session.GetString("IsAdmin") == "true" || (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"));
        }

        // Public registration. Audience restrictions are validated on the server, not only in the form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int eventId, string studentId, string studentName, string schoolLevel, string? gradeLevel, string? course, string section)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            if (DateTime.Now > eventItem.EndDateTime)
            {
                TempData["Error"] = $"Registration is closed. The event '{eventItem.Title}' has already ended.";
                return RedirectToAction(nameof(Index));
            }

            schoolLevel = (schoolLevel ?? string.Empty).Trim();
            gradeLevel = (gradeLevel ?? string.Empty).Trim();
            course = (course ?? string.Empty).Trim();
            section = (section ?? string.Empty).Trim();
            studentId = (studentId ?? string.Empty).Trim();
            studentName = (studentName ?? string.Empty).Trim();

            var validLevels = new[] { "College", "SHS", "JHS" };
            if (!validLevels.Contains(schoolLevel, StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Please select a valid school level.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(section))
            {
                TempData["Error"] = "Please enter your Student ID, full name, and section.";
                return RedirectToAction(nameof(Index));
            }

            if (schoolLevel.Equals("College", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(course))
            {
                TempData["Error"] = "Please enter your college course/program.";
                return RedirectToAction(nameof(Index));
            }
            if (schoolLevel.Equals("SHS", StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(gradeLevel) || string.IsNullOrWhiteSpace(course)))
            {
                TempData["Error"] = "Please select your SHS grade level and strand/program (e.g. ICT or STEM).";
                return RedirectToAction(nameof(Index));
            }
            if (schoolLevel.Equals("JHS", StringComparison.OrdinalIgnoreCase) && !new[] { "Grade 7", "Grade 8", "Grade 9", "Grade 10" }.Contains(gradeLevel))
            {
                TempData["Error"] = "Please select a valid JHS grade level.";
                return RedirectToAction(nameof(Index));
            }
            if (schoolLevel.Equals("SHS", StringComparison.OrdinalIgnoreCase) && !new[] { "Grade 11", "Grade 12" }.Contains(gradeLevel))
            {
                TempData["Error"] = "Please select Grade 11 or Grade 12.";
                return RedirectToAction(nameof(Index));
            }

            if (!IsEligibleForEvent(eventItem, schoolLevel, gradeLevel, course, section))
            {
                TempData["Error"] = "You are not eligible to register for this event based on its target school level, grade, program/strand, or section.";
                return RedirectToAction(nameof(Index));
            }

            var cleanStudentId = studentId;
            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId.ToLower() == cleanStudentId.ToLower());

            if (existingRegistration != null)
                return RedirectToAction("Ticket", "Events", new { id = existingRegistration.Id });

            var registration = new Registration
            {
                EventId = eventId,
                UserId = cleanStudentId,
                StudentName = studentName,
                SchoolLevel = schoolLevel,
                GradeLevel = gradeLevel,
                Course = course,
                Section = section,
                QRCodeToken = Guid.NewGuid().ToString("N"),
                RegisteredAt = DateTime.Now
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();
            return RedirectToAction("Ticket", "Events", new { id = registration.Id });
        }

        private static bool IsEligibleForEvent(Event eventItem, string schoolLevel, string? gradeLevel, string? program, string? section)
        {
            bool Matches(string? target, string? value) => string.IsNullOrWhiteSpace(target) || target.Equals("All", StringComparison.OrdinalIgnoreCase) || string.Equals(target.Trim(), value?.Trim(), StringComparison.OrdinalIgnoreCase);
            return Matches(eventItem.TargetSchoolLevel, schoolLevel)
                && Matches(eventItem.TargetGradeLevel, gradeLevel)
                && Matches(eventItem.TargetProgram, program)
                && Matches(eventItem.TargetSection, section);
        }

        private void ValidateEventAudience(Event eventItem)
        {
            var levels = new[] { "All", "College", "SHS", "JHS" };
            if (!levels.Contains(eventItem.TargetSchoolLevel, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(eventItem.TargetSchoolLevel), "Choose All, College, SHS, or JHS.");
            if (string.Equals(eventItem.TargetSchoolLevel, "All", StringComparison.OrdinalIgnoreCase))
            {
                eventItem.TargetGradeLevel = null;
                eventItem.TargetProgram = null;
                eventItem.TargetSection = null;
            }
            else
            {
                eventItem.TargetGradeLevel = string.IsNullOrWhiteSpace(eventItem.TargetGradeLevel) ? null : eventItem.TargetGradeLevel.Trim();
                eventItem.TargetProgram = string.IsNullOrWhiteSpace(eventItem.TargetProgram) ? null : eventItem.TargetProgram.Trim();
                if (string.Equals(eventItem.TargetSchoolLevel, "JHS", StringComparison.OrdinalIgnoreCase))
                    eventItem.TargetProgram = null;
                eventItem.TargetSection = string.IsNullOrWhiteSpace(eventItem.TargetSection) ? null : eventItem.TargetSection.Trim();
                var grade = eventItem.TargetGradeLevel;
                if (string.Equals(eventItem.TargetSchoolLevel, "SHS", StringComparison.OrdinalIgnoreCase)
                    && grade != null && !new[] { "Grade 11", "Grade 12" }.Contains(grade, StringComparer.OrdinalIgnoreCase))
                    ModelState.AddModelError(nameof(eventItem.TargetGradeLevel), "SHS events can target Grade 11 or Grade 12.");
                if (string.Equals(eventItem.TargetSchoolLevel, "JHS", StringComparison.OrdinalIgnoreCase)
                    && grade != null && !new[] { "Grade 7", "Grade 8", "Grade 9", "Grade 10" }.Contains(grade, StringComparer.OrdinalIgnoreCase))
                    ModelState.AddModelError(nameof(eventItem.TargetGradeLevel), "JHS events can target Grade 7 through Grade 10.");
                if (string.Equals(eventItem.TargetSchoolLevel, "College", StringComparison.OrdinalIgnoreCase)
                    && grade != null && !new[] { "1st Year", "2nd Year", "3rd Year", "4th Year", "5th Year" }.Contains(grade, StringComparer.OrdinalIgnoreCase))
                    ModelState.AddModelError(nameof(eventItem.TargetGradeLevel), "College events can target 1st Year through 5th Year.");
            }
        }

        public async Task<IActionResult> Ticket(int id)
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration == null) return NotFound();

            return View(registration);
        }
    }
}