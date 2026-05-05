using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    public class PostComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public int PostId { get; set; }
        [ForeignKey("PostId")]
        public Post Post { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        // Yorum altına yorum (nested) — null ise ana yorum
        public int? ParentCommentId { get; set; }
        [ForeignKey("ParentCommentId")]
        public PostComment? ParentComment { get; set; }
        public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? MediaUrl { get; set; }

        [StringLength(50)]
        public string? MediaType { get; set; }

        // Beğeni ilişkisi
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    }
}
