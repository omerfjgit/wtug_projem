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

        [StringLength(500)]
        public string? MediaUrl { get; set; }

        [StringLength(50)]
        public string? MediaType { get; set; }

        public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
        public ICollection<PostView> Views { get; set; } = new List<PostView>();
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    }
}
