using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        [MaxLength(20)]
        public string? TargetClass { get; set; } // Null ise "Tüm Okul", doluysa örn "11/A"

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
        public ICollection<PostView> Views { get; set; } = new List<PostView>();
    }
}
