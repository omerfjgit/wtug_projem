using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Models;
using System.Security.Claims;

namespace NoteTrackerApp.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly AppDbContext _context;

        public CalendarController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.CalendarEvents.ToListAsync();
            var eventList = events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.Date.ToString("yyyy-MM-dd"),
                description = e.Description,
                allDay = true,
                color = "#38bdf8" // Tema rengi (Sky Blue)
            });

            return Json(eventList);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddEvent(string title, DateTime date, string? description)
        {
            var newEvent = new CalendarEvent
            {
                Title = title,
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Description = description
            };

            _context.CalendarEvents.Add(newEvent);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = newEvent.Id, title = newEvent.Title, start = newEvent.Date.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.CalendarEvents.FindAsync(id);
            if (ev != null)
            {
                _context.CalendarEvents.Remove(ev);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
