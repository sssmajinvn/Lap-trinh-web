using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = id;
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

        public IActionResult Booking([FromQuery] string movieId)
        {
            if (string.IsNullOrEmpty(movieId))
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = movieId;
            return View();
        }


        // Custom API endpoint to support search by Name, Genre, or Hashtag
        // Built inside our HomeController to not modify any existing backend files
        [HttpGet]
        public async Task<IActionResult> GetSearchMovies([FromQuery] string? name, [FromQuery] string? genre, [FromQuery] string? hashtag)
        {
            var query = _context.Phims
                .Include(p => p.Matheloais)
                .Include(p => p.Mahashtags)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                var lowerName = name.ToLower();
                query = query.Where(p => p.Tenphim.ToLower().Contains(lowerName));
            }

            if (!string.IsNullOrEmpty(genre))
            {
                var lowerGenre = genre.ToLower();
                query = query.Where(p => p.Matheloais.Any(t => t.Tentheloai.ToLower().Contains(lowerGenre)));
            }

            if (!string.IsNullOrEmpty(hashtag))
            {
                var lowerHashtag = hashtag.ToLower().Replace("#", "").Trim();
                query = query.Where(p => p.Mahashtags.Any(h => h.Tenhashtag.ToLower().Replace(" ", "").Contains(lowerHashtag)));
            }

            var movies = await query.ToListAsync();
            
            var data = movies.Select(p => new
            {
                p.Maphim,
                p.Tenphim,
                p.Mota,
                p.Thoiluong,
                p.Ngayramat,
                p.PosterUrl,
                p.TrailerUrl,
                p.Gioihantuoi,
                p.Trangthai,
                Genres = p.Matheloais.Select(t => t.Tentheloai).ToList(),
                Hashtags = p.Mahashtags.Select(h => h.Tenhashtag).ToList()
            });

            return Json(new { status = "success", data });
        }

        // Custom API endpoint to fetch all unique genres to populate filter dropdowns dynamically
        [HttpGet]
        public async Task<IActionResult> GetAllGenres()
        {
            var genres = await _context.Theloais
                .Select(t => t.Tentheloai)
                .Distinct()
                .ToListAsync();
            return Json(new { status = "success", data = genres });
        }
    }
}
