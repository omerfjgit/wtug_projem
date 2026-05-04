using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using System.Security.Claims;
using System.IO;
using NoteTrackerApp.Models;

namespace NoteTrackerApp.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var student = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return NotFound("Öğrenci profili bulunamadı.");
            }

            var classGrades = await _context.Grades
                .Include(g => g.Student)
                .Where(g => g.Student.ClassSection == student.ClassSection)
                .ToListAsync();

            var classAverages = classGrades
                .GroupBy(g => g.CourseName)
                .ToDictionary(
                    g => g.Key, 
                    g => (double)g.Average(x => x.Average)
                );

            ViewBag.ClassAverages = classAverages;

            return View(student);
        }

        public IActionResult GpaCalculator()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photoFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.CanChangePhoto)
            {
                return RedirectToAction(nameof(Index)); // Yetkisi yoksa veya user yoksa reddet
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
                
                user.PhotoUrl = "/uploads/" + fileName;
                _context.Update(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Announcements()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var announcements = await _context.Announcements.OrderByDescending(a => a.CreatedAt).ToListAsync();
            
            var viewedIds = await _context.AnnouncementViews
                .Where(v => v.UserId == userId)
                .Select(v => v.AnnouncementId)
                .ToListAsync();
                
            var unviewedIds = announcements.Select(a => a.Id).Except(viewedIds).ToList();

            if (unviewedIds.Any())
            {
                foreach (var id in unviewedIds)
                {
                    _context.AnnouncementViews.Add(new AnnouncementView { AnnouncementId = id, UserId = userId });
                }
                await _context.SaveChangesAsync();
            }

            return View(announcements);
        }

        public async Task<IActionResult> Discussions()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null) return NotFound();

            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Views)
                .Where(p => string.IsNullOrEmpty(p.TargetClass) || p.TargetClass == student.ClassSection)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Student = student;
            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(string content, string? targetClass, IFormFile? mediaFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = new Post { Content = content, TargetClass = targetClass, UserId = userId };

            if (mediaFile != null && mediaFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await mediaFile.CopyToAsync(fileStream);
                }
                
                post.MediaUrl = "/uploads/" + fileName;
                if (mediaFile.ContentType.StartsWith("image/")) post.MediaType = "image";
                else if (mediaFile.ContentType.StartsWith("video/")) post.MediaType = "video";
                else post.MediaType = "file";
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Discussions));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if(post != null) {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Discussions));
        }

        public async Task<IActionResult> PostDetails(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Include(p => p.Views).ThenInclude(v => v.User)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if(post == null) return NotFound();
            
            // Eğer sınıf bazlıysa ve öğrenci o sınıfta değilse erişemez
            if (!string.IsNullOrEmpty(post.TargetClass) && student?.ClassSection != post.TargetClass && post.UserId != userId) {
                return Unauthorized();
            }
            
            // Register view (duplicate guard)
            if(!await _context.PostViews.AnyAsync(v => v.PostId == id && v.UserId == userId)) {
                _context.PostViews.Add(new PostView { PostId = id, UserId = userId });
                await _context.SaveChangesAsync();
            }
            
            // Reload post with fresh views after possible insert
            post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Include(p => p.Views).ThenInclude(v => v.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            if(post == null) return NotFound();
            
            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content, IFormFile? mediaFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var comment = new PostComment { PostId = postId, UserId = userId, Content = content };

            if (mediaFile != null && mediaFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await mediaFile.CopyToAsync(fileStream);
                }
                
                comment.MediaUrl = "/uploads/" + fileName;
                if (mediaFile.ContentType.StartsWith("image/")) comment.MediaType = "image";
                else if (mediaFile.ContentType.StartsWith("video/")) comment.MediaType = "video";
                else comment.MediaType = "file";
            }

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PostDetails), new { id = postId });
        }
    }
}
