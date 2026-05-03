using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Models;
using System.IO;

namespace NoteTrackerApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> Index()
        {
            var studentCount = await _context.Students.CountAsync();
            var averageGrade = await _context.Grades.AnyAsync() ? await _context.Grades.AverageAsync(g => g.Average) : 0;
            ViewBag.StudentCount = studentCount;
            ViewBag.AverageGrade = averageGrade.ToString("0.##");
            ViewBag.AnnouncementsCount = await _context.Announcements.CountAsync();
            return View();
        }

        // --- STUDENTS ---
        public async Task<IActionResult> Students()
        {
            var students = await _context.Students.Include(s => s.User).ToListAsync();
            return View(students);
        }

        public IActionResult CreateStudent()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudent(string username, string password, string fullName, string studentNumber, string classSection)
        {
            if (ModelState.IsValid)
            {
                // Rastgele 6 haneli kurtarma pini oluştur (Harf ve Rakam)
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var pin = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());

                var user = new AppUser
                {
                    Username = username,
                    Role = "Student",
                    FullName = fullName,
                    RecoveryPin = pin,
                    CanChangePhoto = true
                };
                user.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(user, password);
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var student = new Student
                {
                    UserId = user.Id,
                    StudentNumber = studentNumber,
                    ClassSection = classSection
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Students));
            }
            return View();
        }

        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> EditStudent(int id, string fullName, string studentNumber, string classSection, bool canChangePhoto, string? newPassword, IFormFile? photoFile)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return NotFound();

            student.StudentNumber = studentNumber;
            student.ClassSection = classSection;
            student.User.FullName = fullName;
            student.User.CanChangePhoto = canChangePhoto;

            if (!string.IsNullOrEmpty(newPassword))
            {
                student.User.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(student.User, newPassword);
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(fileStream);
                }
                student.User.PhotoUrl = "/uploads/" + fileName;
            }

            _context.Update(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Students));
        }

        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.Include(s => s.User).Include(s => s.Grades).FirstOrDefaultAsync(s => s.Id == id);
            if (student != null)
            {
                _context.Grades.RemoveRange(student.Grades);
                _context.Students.Remove(student);
                _context.Users.Remove(student.User);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Students));
        }

        // --- GRADES --- (Aynı Kalıyor)
        public async Task<IActionResult> Grades()
        {
            var grades = await _context.Grades.Include(g => g.Student).ThenInclude(s => s.User).ToListAsync();
            return View(grades);
        }

        public async Task<IActionResult> CreateGrade()
        {
            ViewBag.Students = await _context.Students.Include(s => s.User).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGrade(Grade grade)
        {
            grade.CalculateAverage();
            grade.CreatedAt = DateTime.Now;
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Grades));
        }

        // --- ANNOUNCEMENTS ---
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements.Include(a => a.Views).OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(announcements);
        }

        public IActionResult CreateAnnouncement()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(Announcement announcement)
        {
            announcement.CreatedAt = DateTime.Now;
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Announcements));
        }

        public async Task<IActionResult> EditAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();
            return View(announcement);
        }

        [HttpPost]
        public async Task<IActionResult> EditAnnouncement(Announcement updatedAnnouncement)
        {
            var announcement = await _context.Announcements.FindAsync(updatedAnnouncement.Id);
            if (announcement == null) return NotFound();

            announcement.Title = updatedAnnouncement.Title;
            announcement.Content = updatedAnnouncement.Content;
            announcement.Category = updatedAnnouncement.Category;
            
            // Görüldü sıfırlama (Yeniden düzenlendiği için herkes tekrar görmeli)
            var views = _context.AnnouncementViews.Where(v => v.AnnouncementId == announcement.Id);
            _context.AnnouncementViews.RemoveRange(views);

            _context.Update(announcement);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Announcements));
        }

        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Announcements));
        }

        public async Task<IActionResult> AnnouncementViewers(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            var viewers = await _context.AnnouncementViews
                .Include(v => v.User)
                .Where(v => v.AnnouncementId == id)
                .OrderByDescending(v => v.ViewedAt)
                .ToListAsync();
                
            var userIds = viewers.Select(v => v.UserId).ToList();
            var students = await _context.Students.Where(s => userIds.Contains(s.UserId)).ToDictionaryAsync(s => s.UserId, s => s);

            ViewBag.Students = students;
            ViewBag.Announcement = announcement;
            return View(viewers);
        }

        // --- DISCUSSIONS (SOCIAL) ---
        public async Task<IActionResult> Discussions()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Views)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Discussions));
        }
    }
}
