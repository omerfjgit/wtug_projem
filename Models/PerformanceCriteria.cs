using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    /// <summary>
    /// Sözlü/Performans notunun alt kırılımlarını tutar.
    /// Öğretmen her öğrenci için ders bazlı detaylı performans girişi yapabilir.
    /// </summary>
    public class PerformanceCriteria
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
        [StringLength(20)]
        [Display(Name = "Dönem")]
        public string Semester { get; set; } // "1. Dönem" / "2. Dönem"

        [Range(0, 10)]
        [Display(Name = "Derse Katılım")]
        public int Participation { get; set; } // 0-10 puan

        [Range(0, 10)]
        [Display(Name = "Ödev Yapma")]
        public int HomeworkCompletion { get; set; } // 0-10 puan

        [Range(0, 10)]
        [Display(Name = "Kılık Kıyafet / Tutum")]
        public int Appearance { get; set; } // 0-10 puan

        [Range(0, 10)]
        [Display(Name = "Materyal Getirme")]
        public int MaterialReady { get; set; } // 0-10 puan

        [StringLength(500)]
        [Display(Name = "Öğretmen Notu")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Toplam puan (max 40)
        [NotMapped]
        public int TotalScore => Participation + HomeworkCompletion + Appearance + MaterialReady;

        // Yüzdelik skor (0-100 arası dönüştürülmüş)
        [NotMapped]
        public decimal ScorePercent => Math.Round((decimal)TotalScore / 40 * 100, 1);
    }
}
