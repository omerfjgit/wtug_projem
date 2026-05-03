using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Models;

namespace NoteTrackerApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<AnnouncementView> AnnouncementViews { get; set; }
        
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<PostView> PostViews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            var adminUser = new AppUser 
            { 
                Id = 1, 
                Username = "admin", 
                Role = "Admin", 
                FullName = "Sistem Yöneticisi",
                CanChangePhoto = true
            };
            // Hash the password "123"
            adminUser.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(adminUser, "123");

            // Seed Admin User
            modelBuilder.Entity<AppUser>().HasData(adminUser);
        }
    }
}
