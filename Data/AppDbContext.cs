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
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<PerformanceCriteria> PerformanceCriterias { get; set; }
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Self-referencing PostComment (nested replies) — cycle'ı engelle
            modelBuilder.Entity<PostComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // PostLike: bir kullanıcı bir gönderi veya yoruma yalnızca bir kez oy verebilir
            modelBuilder.Entity<PostLike>()
                .HasIndex(l => new { l.UserId, l.PostId })
                .IsUnique()
                .HasFilter("`PostId` IS NOT NULL");

            modelBuilder.Entity<PostLike>()
                .HasIndex(l => new { l.UserId, l.CommentId })
                .IsUnique()
                .HasFilter("`CommentId` IS NOT NULL");

            var adminUser = new AppUser 
            { 
                Id = 1, 
                Username = "admin", 
                Role = "Admin", 
                FullName = "Sistem Yöneticisi",
                CanChangePhoto = true
            };
            // Hash the password "webKontrol@admin"
            adminUser.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(adminUser, "webKontrol@admin");

            // Seed Admin User
            modelBuilder.Entity<AppUser>().HasData(adminUser);

            // Seed Turkish National Holidays & School Important Dates
            static DateTime UTC(int y, int m, int d) => DateTime.SpecifyKind(new DateTime(y, m, d), DateTimeKind.Utc);

            modelBuilder.Entity<CalendarEvent>().HasData(
                new CalendarEvent { Id = 1,  Title = "Yılbaşı",                                              Date = UTC(2025, 1,  1),  Description = "Yeni Yıl" },
                new CalendarEvent { Id = 2,  Title = "23 Nisan Ulusal Egemenlik ve Çocuk Bayramı",     Date = UTC(2025, 4,  23), Description = "Ulusal Egemenlik ve Çocuk Bayramı" },
                new CalendarEvent { Id = 3,  Title = "1 Mayıs İşçi Bayramı",                              Date = UTC(2025, 5,  1),  Description = "Emek ve Dayanışma Günü" },
                new CalendarEvent { Id = 4,  Title = "19 Mayıs Atatürk'ü Anma, Gençlik ve Spor Bayramı", Date = UTC(2025, 5,  19), Description = "Gençlik ve Spor Bayramı" },
                new CalendarEvent { Id = 5,  Title = "15 Temmuz Demokrasi ve Milli Birlik Günü",    Date = UTC(2025, 7,  15), Description = "Milli Birlik Günü" },
                new CalendarEvent { Id = 6,  Title = "30 Ağustos Zafer Bayramı",                       Date = UTC(2025, 8,  30), Description = "Zafer Bayramı" },
                new CalendarEvent { Id = 7,  Title = "29 Ekim Cumhuriyet Bayramı",                    Date = UTC(2025, 10, 29), Description = "Türkiye Cumhuriyeti'nin Kuruluş Yıldönümü" },
                new CalendarEvent { Id = 8,  Title = "10 Kasım - Atatürk'ü Anma Günü",              Date = UTC(2025, 11, 10), Description = "Mustafa Kemal Atatürk'ü saygı ve özlemle anıyoruz" },
                new CalendarEvent { Id = 9,  Title = "I. Dönem Başlangıcı",                            Date = UTC(2024, 9,  16), Description = "2024-2025 eğitim-öğretim yılı başlar" },
                new CalendarEvent { Id = 10, Title = "I. Dönem Sonu",                                  Date = UTC(2025, 1,  17), Description = "1. Dönem sona erer" },
                new CalendarEvent { Id = 11, Title = "II. Dönem Başlangıcı",                           Date = UTC(2025, 2,  3),  Description = "2. Dönem başlar" },
                new CalendarEvent { Id = 12, Title = "II. Dönem Sonu / Yıl Sonu",                     Date = UTC(2025, 6,  13), Description = "2024-2025 eğitim öğretim yılı sona erer" },
                new CalendarEvent { Id = 13, Title = "Ramazan Bayramı",                                Date = UTC(2025, 3,  30), Description = "Ramazan Bayramı (3 Gün)" },
                new CalendarEvent { Id = 14, Title = "Kurban Bayramı",                                 Date = UTC(2025, 6,  6),  Description = "Kurban Bayramı (4 Gün)" }
            );
        }
    }
}
