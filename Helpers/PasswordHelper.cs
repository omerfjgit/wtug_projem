using Microsoft.AspNetCore.Identity;
using NoteTrackerApp.Models;

namespace NoteTrackerApp.Helpers
{
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<AppUser> _passwordHasher = new PasswordHasher<AppUser>();

        public static string HashPassword(AppUser user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        public static bool VerifyPassword(AppUser user, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, providedPassword);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
