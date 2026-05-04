using System;
using System.ComponentModel.DataAnnotations;

namespace NoteTrackerApp.Models
{
    public class CalendarEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
