using NoteTrackerApp.Models;
namespace NoteTrackerApp.Helpers
{
    public class CommentViewModel
    {
        public PostComment Comment { get; set; }
        public int PostId { get; set; }
        public int CurrentUserId { get; set; }
        public List<PostLike> UserLikes { get; set; } = new();
        public bool IsAdmin { get; set; }
    }
}
