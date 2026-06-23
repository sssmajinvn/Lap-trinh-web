using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to format movie response
        private static object FormatMovie(Phim p)
        {
            return new
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
                p.Duoctaoboi,
                p.Duoctaongay,
                p.TmdbId,
                Genres = p.Matheloais.Select(t => t.Tentheloai).ToList(),
                Directors = p.Madaodiens.Select(d => new { name = d.Tendaodien, avatar_url = d.Urlanhdaidien }).ToList(),
                Actors = p.Madienviens.Select(a => new { name = a.Tendienvien, avatar_url = a.Urlanhdaidien }).ToList()
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetMovies([FromQuery] string? status)
        {
            var query = _context.Phims
                .Include(p => p.Matheloais)
                .Include(p => p.Madaodiens)
                .Include(p => p.Madienviens)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Trangthai == status);
            }

            var movies = await query.ToListAsync();
            var data = movies.Select(FormatMovie);

            return Ok(new { status = "success", data });
        }

        [HttpGet("hot")]
        public async Task<IActionResult> GetHotMovies()
        {
            var hotMovies = await _context.Phims
                .Include(p => p.Matheloais)
                .Include(p => p.Madaodiens)
                .Include(p => p.Madienviens)
                .Include(p => p.Mahashtags)
                .Where(p => (p.Trangthai == "showing" || p.Trangthai == "now_showing") &&
                            p.Mahashtags.Any(h => h.Tenhashtag.ToLower().Replace(" ", "").Contains("phimhot")))
                .Take(5)
                .ToListAsync();

            var data = hotMovies.Select(FormatMovie);

            return Ok(new { status = "success", data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovieById(string id)
        {
            var movie = await _context.Phims
                .Include(p => p.Matheloais)
                .Include(p => p.Madaodiens)
                .Include(p => p.Madienviens)
                .FirstOrDefaultAsync(p => p.Maphim == id);

            if (movie == null)
                return NotFound(new { status = "error", message = "Không tìm thấy phim" });

            return Ok(new { status = "success", data = FormatMovie(movie) });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMovies([FromQuery] string? name, [FromQuery] string? genre)
        {
            var query = _context.Phims
                .Include(p => p.Matheloais)
                .Include(p => p.Madaodiens)
                .Include(p => p.Madienviens)
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

            var movies = await query.ToListAsync();
            var data = movies.Select(FormatMovie);

            return Ok(new { status = "success", data });
        }
    }
}
