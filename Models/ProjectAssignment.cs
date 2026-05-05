using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    public enum ProjectStatus
    {
        Atandı = 0,
        Taslak = 1,
        AraKontrol = 2,
        NihaiFazı = 3,
        Teslim = 4
    }

    /// <summary>
    /// Lisede zorunlu olan yıllık proje ödevinin adım adım takibini sağlar.
    /// </summary>
    public class ProjectAssignment
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Ders Adı")]
        public string CourseName { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Proje Konusu")]
        public string Topic { get; set; }

        [Display(Name = "Durum")]
        public ProjectStatus Status { get; set; } = ProjectStatus.Atandı;

        [Display(Name = "Taslak Teslim Tarihi")]
        public DateTime? DraftDueDate { get; set; }

        [Display(Name = "Ara Kontrol Tarihi")]
        public DateTime? CheckDate { get; set; }

        [Display(Name = "Nihai Teslim Tarihi")]
        public DateTime? FinalDueDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Öğretmen Geri Bildirimi")]
        public string? TeacherNote { get; set; }

        [StringLength(1000)]
        [Display(Name = "Öğrenci Notu")]
        public string? StudentNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
