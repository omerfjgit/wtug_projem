using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteTrackerApp.Models
{
    /// <summary>
    /// Tartışma gönderisi veya yoruma verilen like/dislike oyunu tutar.
    /// </summary>
    public class PostLike
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        // Gönderi veya yorum için (biri null olacak)
        public int? PostId { get; set; }
        [ForeignKey("PostId")]
        public Post? Post { get; set; }

        public int? CommentId { get; set; }
        [ForeignKey("CommentId")]
        public PostComment? Comment { get; set; }

        /// <summary>true = Like, false = Dislike</summary>
        public bool IsLike { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
