using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    public class AnnouncementView
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnnouncementId { get; set; }
        
        [ForeignKey("AnnouncementId")]
        public Announcement Announcement { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;
    }
}
