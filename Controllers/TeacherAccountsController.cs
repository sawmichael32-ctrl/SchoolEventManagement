using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SchoolEventManagement.Data;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Controllers
{
    // Admin-only screen: create teacher accounts and assign the courses they handle
    public class TeacherAccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Teacher> _hasher = new();

        public TeacherAccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdminUser()
        {
            return HttpContext.Session.GetString("IsAdmin") == "true" || (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"));
        }

        private static List<string> ParseCourses(string? text)
        {
            return (text ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => c.Length > 0 && c.Length <= 100)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Treat names as duplicates even when capitalization or extra spaces differ.
        private static string NormalizeName(string? name)
        {
            return Regex.Replace((name ?? string.Empty).Trim(), @"\s+", " ")
                .ToUpperInvariant();
        }

        private async Task<bool> FullNameAlreadyExistsAsync(string? fullName, int? excludeTeacherId = null)
        {
            var normalizedName = NormalizeName(fullName);
            if (normalizedName.Length == 0) return false;

            // Normalize in memory so repeated spaces inside names are handled consistently.
            var existingNames = await _context.Teachers
                .Where(t => excludeTeacherId == null || t.Id != excludeTeacherId.Value)
                .Select(t => t.FullName)
                .ToListAsync();

            return existingNames.Any(name => NormalizeName(name) == normalizedName);
        }

        // GET: TeacherAccounts
        public async Task<IActionResult> Index()
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin", "Events");

            var teachers = await _context.Teachers
                .Include(t => t.Courses)
                .OrderBy(t => t.FullName)
                .ToListAsync();
            return View(teachers);
        }

        // GET: TeacherAccounts/Create
        public IActionResult Create()
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin", "Events");
            ViewBag.IsEdit = false;
            return View("Form", new TeacherFormViewModel());
        }

        // POST: TeacherAccounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeacherFormViewModel vm)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin", "Events");

            var username = vm.Username?.Trim() ?? string.Empty;
            vm.FullName = Regex.Replace((vm.FullName ?? string.Empty).Trim(), @"\s+", " ");

            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "Password is required.");

            if (await FullNameAlreadyExistsAsync(vm.FullName))
                ModelState.AddModelError(nameof(vm.FullName),
                    "A teacher with this full name already exists. Please use the existing teacher account.");

            if (username.Length > 0 && await _context.Teachers.AnyAsync(t => t.Username.ToLower() == username.ToLower()))
                ModelState.AddModelError(nameof(vm.Username), "That username is already taken.");

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = false;
                return View("Form", vm);
            }

            var teacher = new Teacher { FullName = vm.FullName.Trim(), Username = username };
            teacher.PasswordHash = _hasher.HashPassword(teacher, vm.Password!);
            foreach (var course in ParseCourses(vm.CoursesText))
                teacher.Courses.Add(new TeacherCourse { CourseName = course });

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Teacher account '{teacher.Username}' created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: TeacherAccounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin", "Events");
            if (id == null) return NotFound();

            var teacher = await _context.Teachers.Include(t => t.Courses).FirstOrDefaultAsync(t => t.Id == id);
            if (teacher == null) return NotFound();

            ViewBag.IsEdit = true;
            return View("Form", new TeacherFormViewModel
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Username = teacher.Username,
                CoursesText = string.Join("\n", teacher.Courses.Select(c => c.CourseName).OrderBy(n => n))
            });
        }

        // POST: TeacherAccounts/Edit/5  (leave Password blank to keep the current one)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeacherFormViewModel vm)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin", "Events");

            var teacher = await _context.Teachers.Include(t => t.Courses).FirstOrDefaultAsync(t => t.Id == id);
            if (teacher == null) return NotFound();

            // Username is not editable; it only appears in the form for display
            ModelState.Remove(nameof(vm.Username));

            vm.FullName = Regex.Replace((vm.FullName ?? string.Empty).Trim(), @"\s+", " ");

            if (string.IsNullOrWhiteSpace(vm.FullName))
                ModelState.AddModelError(nameof(vm.FullName), "Full name is required.");
            else if (await FullNameAlreadyExistsAsync(vm.FullName, id))
                ModelState.AddModelError(nameof(vm.FullName),
                    "Another teacher with this full name already exists. Please enter a different name.");

            if (!ModelState.IsValid)
            {
                vm.Id = id;
                vm.Username = teacher.Username;
                ViewBag.IsEdit = true;
                return View("Form", vm);
            }

            teacher.FullName = vm.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(vm.Password))
                teacher.PasswordHash = _hasher.HashPassword(teacher, vm.Password);

            _context.TeacherCourses.RemoveRange(teacher.Courses);
            foreach (var course in ParseCourses(vm.CoursesText))
                _context.TeacherCourses.Add(new TeacherCourse { TeacherId = teacher.Id, CourseName = course });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Teacher '{teacher.Username}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: TeacherAccounts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminUser()) return RedirectToAction("AdminLogin", "Events");

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Teacher account '{teacher.Username}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
