using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Models;
using System.IO;
using System.Security.Claims;

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

        // ─── DASHBOARD ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var studentCount = await _context.Students.CountAsync();
            var averageGrade = await _context.Grades.AnyAsync() ? await _context.Grades.AverageAsync(g => g.Average) : 0;
            ViewBag.StudentCount = studentCount;
            ViewBag.AverageGrade = averageGrade.ToString("0.00");
            ViewBag.AnnouncementsCount = await _context.Announcements.CountAsync();

            var classAverages = await _context.Grades
                .Include(g => g.Student)
                .GroupBy(g => new { g.Student.ClassSection, g.CourseName })
                .Select(g => new {
                    ClassSection = g.Key.ClassSection,
                    CourseName = g.Key.CourseName,
                    Average = g.Average(x => x.Average)
                })
                .OrderBy(g => g.ClassSection)
                .ThenBy(g => g.CourseName)
                .ToListAsync();
            
            ViewBag.ClassAverages = classAverages;
            return View();
        }

        // ─── PROFILE ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, string? newPassword, IFormFile? photoFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FullName = fullName;

            if (!string.IsNullOrEmpty(newPassword))
                user.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(user, newPassword);

            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                using var fs = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                await photoFile.CopyToAsync(fs);
                user.PhotoUrl = "/uploads/" + fileName;
            }

            _context.Update(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Profil başarıyla güncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        // ─── STUDENTS ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Students(string? search)
        {
            ViewBag.Search = search;
            var query = _context.Students.Include(s => s.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(s =>
                    s.User.FullName.ToLower().Contains(lower) ||
                    s.User.Username.ToLower().Contains(lower) ||
                    s.StudentNumber.Contains(lower) ||
                    s.ClassSection.ToLower().Contains(lower));
            }

            var students = await query.OrderBy(s => s.ClassSection).ThenBy(s => s.User.FullName).ToListAsync();
            return View(students);
        }

        public IActionResult CreateStudent() => View();

        [HttpPost]
        public async Task<IActionResult> CreateStudent(string username, string password, string fullName, string studentNumber, string classSection)
        {
            if (ModelState.IsValid)
            {
                if (username.ToLower().Contains("admin") || fullName.ToLower().Contains("admin"))
                {
                    ModelState.AddModelError("", "Öğrenci adı veya kullanıcı adı 'admin' kelimesini içeremez.");
                    return View();
                }

                bool userExists = await _context.Users.AnyAsync(u => u.Username == username || u.FullName == fullName);
                bool studentExists = await _context.Students.AnyAsync(s => s.StudentNumber == studentNumber);

                if (userExists || studentExists)
                {
                    ModelState.AddModelError("", "Bu kullanıcı adı, isim veya öğrenci numarası sistemde zaten kayıtlı.");
                    return View();
                }

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

                var student = new Student { UserId = user.Id, StudentNumber = studentNumber, ClassSection = classSection };
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
        public async Task<IActionResult> EditStudent(int id, string fullName, string studentNumber, string classSection, bool canChangePhoto, string? newPassword, IFormFile? photoFile, bool removePhoto = false)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return NotFound();

            student.StudentNumber = studentNumber;
            student.ClassSection = classSection;
            student.User.FullName = fullName;
            student.User.CanChangePhoto = canChangePhoto;

            if (!string.IsNullOrEmpty(newPassword))
                student.User.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(student.User, newPassword);

            if (removePhoto)
                student.User.PhotoUrl = null;

            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                using var fs = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                await photoFile.CopyToAsync(fs);
                student.User.PhotoUrl = "/uploads/" + fileName;
            }

            _context.Update(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Students));
        }

        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student != null)
            {
                _context.Grades.RemoveRange(student.Grades);
                _context.Students.Remove(student);
                _context.Users.Remove(student.User);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Students));
        }

        // ─── GRADES ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Grades(string? search)
        {
            ViewBag.Search = search;
            var query = _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(g =>
                    g.Student.User.FullName.ToLower().Contains(lower) ||
                    g.Student.StudentNumber.Contains(lower) ||
                    g.CourseName.ToLower().Contains(lower) ||
                    g.Student.ClassSection.ToLower().Contains(lower));
            }

            var grades = await query.OrderBy(g => g.Student.ClassSection).ThenBy(g => g.Student.User.FullName).ToListAsync();
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

        // ─── PERFORMANCE CRITERIA ────────────────────────────────────────────────
        public async Task<IActionResult> PerformanceCriteria(string? search)
        {
            ViewBag.Search = search;
            var query = _context.PerformanceCriterias
                .Include(p => p.Student).ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(p =>
                    p.Student.User.FullName.ToLower().Contains(lower) ||
                    p.Student.StudentNumber.Contains(lower) ||
                    p.CourseName.ToLower().Contains(lower));
            }

            var items = await query.OrderBy(p => p.Student.ClassSection).ThenBy(p => p.Student.User.FullName).ToListAsync();
            ViewBag.Students = await _context.Students.Include(s => s.User).ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePerformanceCriteria(PerformanceCriteria model)
        {
            _context.PerformanceCriterias.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Performans kriteri kaydedildi.";
            return RedirectToAction(nameof(PerformanceCriteria));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePerformanceCriteria(int id)
        {
            var item = await _context.PerformanceCriterias.FindAsync(id);
            if (item != null) { _context.PerformanceCriterias.Remove(item); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(PerformanceCriteria));
        }

        // ─── PROJECT ASSIGNMENTS ─────────────────────────────────────────────────
        public async Task<IActionResult> ProjectAssignments(string? search)
        {
            ViewBag.Search = search;
            var query = _context.ProjectAssignments
                .Include(p => p.Student).ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(p =>
                    p.Student.User.FullName.ToLower().Contains(lower) ||
                    p.Topic.ToLower().Contains(lower) ||
                    p.CourseName.ToLower().Contains(lower));
            }

            var items = await query.OrderBy(p => p.CreatedAt).ToListAsync();
            ViewBag.Students = await _context.Students.Include(s => s.User).ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProjectAssignment(ProjectAssignment model)
        {
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            _context.ProjectAssignments.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Proje ödevi oluşturuldu.";
            return RedirectToAction(nameof(ProjectAssignments));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProjectStatus(int id, ProjectStatus status, string? teacherNote)
        {
            var item = await _context.ProjectAssignments.FindAsync(id);
            if (item != null)
            {
                item.Status = status;
                if (teacherNote != null) item.TeacherNote = teacherNote;
                item.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ProjectAssignments));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProjectAssignment(int id)
        {
            var item = await _context.ProjectAssignments.FindAsync(id);
            if (item != null) { _context.ProjectAssignments.Remove(item); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(ProjectAssignments));
        }

        // ─── ANNOUNCEMENTS ───────────────────────────────────────────────────────
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements.Include(a => a.Views).OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(announcements);
        }

        public IActionResult CreateAnnouncement() => View();

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(Announcement announcement, IFormFile? mediaFile)
        {
            if (mediaFile != null && mediaFile.Length > 0)
            {
                var url = await SaveUpload(mediaFile);
                announcement.MediaUrl = url;
                announcement.MediaType = GetMediaType(mediaFile.ContentType);
            }

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
        public async Task<IActionResult> EditAnnouncement(Announcement updatedAnnouncement, IFormFile? mediaFile, bool removeMedia = false)
        {
            var announcement = await _context.Announcements.FindAsync(updatedAnnouncement.Id);
            if (announcement == null) return NotFound();

            announcement.Title = updatedAnnouncement.Title;
            announcement.Content = updatedAnnouncement.Content;
            announcement.Category = updatedAnnouncement.Category;

            if (removeMedia)
            {
                announcement.MediaUrl = null;
                announcement.MediaType = null;
            }

            if (mediaFile != null && mediaFile.Length > 0)
            {
                announcement.MediaUrl = await SaveUpload(mediaFile);
                announcement.MediaType = GetMediaType(mediaFile.ContentType);
            }

            // Görüldü sıfırlama
            var views = _context.AnnouncementViews.Where(v => v.AnnouncementId == announcement.Id);
            _context.AnnouncementViews.RemoveRange(views);

            _context.Update(announcement);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Announcements));
        }

        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null) { _context.Announcements.Remove(announcement); await _context.SaveChangesAsync(); }
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

        // ─── DISCUSSIONS ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Discussions(string? sort)
        {
            ViewBag.Sort = sort ?? "newest";
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Views)
                .Include(p => p.Likes)
                .ToListAsync();

            posts = (sort == "popular")
                ? posts.OrderByDescending(p => p.Likes.Count(l => l.IsLike)).ToList()
                : posts.OrderByDescending(p => p.CreatedAt).ToList();

            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(string content, string? targetClass, IFormFile? mediaFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = new Post { Content = content, TargetClass = targetClass, UserId = userId };

            if (mediaFile != null && mediaFile.Length > 0)
            {
                post.MediaUrl = await SaveUpload(mediaFile);
                post.MediaType = GetMediaType(mediaFile.ContentType);
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Discussions));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post != null)
            {
                _context.PostLikes.RemoveRange(post.Likes);
                _context.PostComments.RemoveRange(post.Comments);
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Discussions));
        }

        public async Task<IActionResult> PostDetails(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Include(p => p.Comments).ThenInclude(c => c.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.Replies).ThenInclude(r => r.User)
                .Include(p => p.Comments).ThenInclude(c => c.Replies).ThenInclude(r => r.Likes)
                .Include(p => p.Views).ThenInclude(v => v.User)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (post == null) return NotFound();

            if (!await _context.PostViews.AnyAsync(v => v.PostId == id && v.UserId == userId))
            {
                _context.PostViews.Add(new PostView { PostId = id, UserId = userId });
                await _context.SaveChangesAsync();
            }

            ViewBag.CurrentUserId = userId;
            ViewBag.UserLikes = await _context.PostLikes.Where(l => l.UserId == userId).ToListAsync();
            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content, IFormFile? mediaFile, int? parentCommentId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var comment = new PostComment { PostId = postId, UserId = userId, Content = content, ParentCommentId = parentCommentId };

            if (mediaFile != null && mediaFile.Length > 0)
            {
                comment.MediaUrl = await SaveUpload(mediaFile);
                comment.MediaType = GetMediaType(mediaFile.ContentType);
            }

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PostDetails), new { id = postId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.PostComments
                .Include(c => c.Replies).ThenInclude(r => r.Likes)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment != null)
            {
                var postId = comment.PostId;
                // Önce alt yorumların beğenilerini sil
                foreach (var reply in comment.Replies)
                    _context.PostLikes.RemoveRange(reply.Likes);
                _context.PostComments.RemoveRange(comment.Replies);
                _context.PostLikes.RemoveRange(comment.Likes);
                _context.PostComments.Remove(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(PostDetails), new { id = postId });
            }
            return RedirectToAction(nameof(Discussions));
        }

        // ─── LIKES ───────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> LikePost(int postId, bool isLike)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existing = await _context.PostLikes.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);

            if (existing != null)
            {
                if (existing.IsLike == isLike)
                    _context.PostLikes.Remove(existing); // Toggle off
                else
                    existing.IsLike = isLike;
            }
            else
            {
                _context.PostLikes.Add(new PostLike { UserId = userId, PostId = postId, IsLike = isLike });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PostDetails), new { id = postId });
        }

        [HttpPost]
        public async Task<IActionResult> LikeComment(int commentId, int postId, bool isLike)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existing = await _context.PostLikes.FirstOrDefaultAsync(l => l.UserId == userId && l.CommentId == commentId);

            if (existing != null)
            {
                if (existing.IsLike == isLike)
                    _context.PostLikes.Remove(existing);
                else
                    existing.IsLike = isLike;
            }
            else
            {
                _context.PostLikes.Add(new PostLike { UserId = userId, CommentId = commentId, IsLike = isLike });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PostDetails), new { id = postId });
        }

        // ─── HELPERS ─────────────────────────────────────────────────────────────
        private async Task<string> SaveUpload(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            using var fs = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
            await file.CopyToAsync(fs);
            return "/uploads/" + fileName;
        }

        private static string GetMediaType(string contentType)
        {
            if (contentType.StartsWith("image/")) return "image";
            if (contentType.StartsWith("video/")) return "video";
            return "file";
        }
    }
}
