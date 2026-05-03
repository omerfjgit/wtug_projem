using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Models;
using System.Security.Claims;

namespace NoteTrackerApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
                if (User.IsInRole("Student")) return RedirectToAction("Index", "Student");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user != null && NoteTrackerApp.Helpers.PasswordHelper.VerifyPassword(user, password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (user.Role == "Admin")
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "Student");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
        
        public IActionResult AccessDenied()
        {
            return View();
        }

        // --- ŞİFREMİ UNUTTUM (KURTARMA KODU İLE) ---
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string username, string recoveryPin, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.RecoveryPin == recoveryPin);
            
            if (user != null)
            {
                user.Password = NoteTrackerApp.Helpers.PasswordHelper.HashPassword(user, newPassword);
                _context.Update(user);
                await _context.SaveChangesAsync();
                
                ViewBag.Success = "Şifreniz başarıyla güncellendi! Yeni şifrenizle giriş yapabilirsiniz.";
                return View("Login");
            }

            ViewBag.Error = "Kullanıcı adı veya Kurtarma Kodu hatalı!";
            return View();
        }
    }
}
