using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        [Required(ErrorMessage = "Öğrenci No zorunludur.")]
        [Display(Name = "Öğrenci No")]
        public string StudentNumber { get; set; }

        [Display(Name = "Sınıf / Şube")]
        [StringLength(20)]
        public string ClassSection { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // İlişkiler
        public ICollection<Grade> Grades { get; set; }
    }
}
