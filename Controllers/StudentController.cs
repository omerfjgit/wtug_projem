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

            if (student == null) return NotFound("Öğrenci profili bulunamadı.");

            var classGrades = await _context.Grades
                .Include(g => g.Student)
                .Where(g => g.Student.ClassSection == student.ClassSection)
                .ToListAsync();

            ViewBag.ClassAverages = classGrades
                .GroupBy(g => g.CourseName)
                .ToDictionary(g => g.Key, g => (double)g.Average(x => x.Average));

            ViewBag.Projects = await _context.ProjectAssignments
                .Where(p => p.StudentId == student.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(student);
        }

        public IActionResult GpaCalculator() => View();

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photoFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.CanChangePhoto) return RedirectToAction(nameof(Index));

            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                using var fs = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                await photoFile.CopyToAsync(fs);
                user.PhotoUrl = "/uploads/" + fileName;
                _context.Update(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── ANNOUNCEMENTS ───────────────────────────────────────────────────────
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
                    _context.AnnouncementViews.Add(new AnnouncementView { AnnouncementId = id, UserId = userId });
                await _context.SaveChangesAsync();
            }

            return View(announcements);
        }

        // ─── DISCUSSIONS ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Discussions(string? sort)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return NotFound();

            ViewBag.Sort = sort ?? "newest";
            ViewBag.Student = student;
            ViewBag.CurrentUserId = userId;

            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Views)
                .Include(p => p.Likes)
                .Where(p => string.IsNullOrEmpty(p.TargetClass) || p.TargetClass == student.ClassSection)
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = await _context.Posts
                .Include(p => p.Comments).ThenInclude(c => c.Likes)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (post != null)
            {
                foreach (var c in post.Comments) _context.PostLikes.RemoveRange(c.Likes);
                _context.PostComments.RemoveRange(post.Comments);
                _context.PostLikes.RemoveRange(post.Likes);
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Discussions));
        }

        public async Task<IActionResult> PostDetails(int id, string? sort)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
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

            if (!string.IsNullOrEmpty(post.TargetClass) && student?.ClassSection != post.TargetClass && post.UserId != userId)
                return Unauthorized();

            if (!await _context.PostViews.AnyAsync(v => v.PostId == id && v.UserId == userId))
            {
                _context.PostViews.Add(new PostView { PostId = id, UserId = userId });
                await _context.SaveChangesAsync();
            }

            ViewBag.Sort = sort ?? "newest";
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var comment = await _context.PostComments
                .Include(c => c.Replies).ThenInclude(r => r.Likes)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (comment != null)
            {
                var postId = comment.PostId;
                foreach (var reply in comment.Replies) _context.PostLikes.RemoveRange(reply.Likes);
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
                if (existing.IsLike == isLike) _context.PostLikes.Remove(existing);
                else existing.IsLike = isLike;
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
                if (existing.IsLike == isLike) _context.PostLikes.Remove(existing);
                else existing.IsLike = isLike;
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
