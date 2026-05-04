using System;
using System.ComponentModel.DataAnnotations;

namespace NoteTrackerApp.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Kategori")]
        public string Category { get; set; } // Örn: Genel, Sınav, Etkinlik, Uyarı

        [Required(ErrorMessage = "Duyuru başlığı zorunludur.")]
        [StringLength(200)]
        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Duyuru içeriği zorunludur.")]
        [Display(Name = "İçerik")]
        public string Content { get; set; }

        [Display(Name = "Yayın Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? MediaUrl { get; set; }

        [StringLength(50)]
        public string? MediaType { get; set; } // "image", "video", "file"


        public ICollection<AnnouncementView> Views { get; set; } = new List<AnnouncementView>();
    }
}
