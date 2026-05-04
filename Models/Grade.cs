using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    public class Grade
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required(ErrorMessage = "Ders Adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ders Adı")]
        public string CourseName { get; set; }

        [Required]
        [Display(Name = "Dönem")]
        public string Semester { get; set; } // "1. Dönem" veya "2. Dönem"

        // MEB Lise Notları
        [Range(0, 100)]
        [Display(Name = "1. Sınav")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Exam1 { get; set; }

        [Range(0, 100)]
        [Display(Name = "2. Sınav")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Exam2 { get; set; }

        [Range(0, 100)]
        [Display(Name = "1. Sözlü")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Oral1 { get; set; }

        [Range(0, 100)]
        [Display(Name = "2. Sözlü")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Oral2 { get; set; }

        [Range(0, 100)]
        [Display(Name = "1. Performans")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Performance1 { get; set; }

        [Range(0, 100)]
        [Display(Name = "2. Performans")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Performance2 { get; set; }

        [Range(0, 100)]
        [Display(Name = "Proje")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Project { get; set; }

        [Display(Name = "Ortalama")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Average { get; set; }

        [Display(Name = "Durum")]
        public bool IsPassed { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public void CalculateAverage()
        {
            int count = 0;
            decimal total = 0;

            var grades = new decimal?[] { Exam1, Exam2, Oral1, Oral2, Performance1, Performance2, Project };

            foreach (var g in grades)
            {
                if (g.HasValue)
                {
                    total += g.Value;
                    count++;
                }
            }

            if (count > 0)
                Average = Math.Round(total / count, 2);
            else
                Average = 0;

            IsPassed = Average >= 50;
        }
    }
}
