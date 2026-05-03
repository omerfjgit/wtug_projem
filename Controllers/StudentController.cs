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

            return View(student);
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
        public async Task<IActionResult> CreatePost(string content, string? targetClass)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = new Post { Content = content, TargetClass = targetClass, UserId = userId };
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
            
            // Register view
            if(!post.Views.Any(v => v.UserId == userId)) {
                _context.PostViews.Add(new PostView { PostId = id, UserId = userId });
                await _context.SaveChangesAsync();
                post.Views.Add(new PostView { UserId = userId, User = await _context.Users.FindAsync(userId) }); // UI için hemen ekle
            }
            
            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            _context.PostComments.Add(new PostComment { PostId = postId, UserId = userId, Content = content });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PostDetails), new { id = postId });
        }
    }
}
