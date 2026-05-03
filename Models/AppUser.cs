using System.ComponentModel.DataAnnotations;

namespace NoteTrackerApp.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı Adı zorunludur.")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; } 

        [Required]
        public string Role { get; set; } // "Admin" veya "Student"

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        // --- YENİ EKLENENLER ---
        
        [StringLength(255)]
        public string? PhotoUrl { get; set; } // Profil Fotoğrafı Yolu

        public bool CanChangePhoto { get; set; } = true; // Fotoğraf değiştirme yetkisi

        [StringLength(10)]
        public string? RecoveryPin { get; set; } // Şifre sıfırlama için Kurtarma Kodu
    }
}
