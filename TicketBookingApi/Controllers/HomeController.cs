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

        public IActionResult Seats([FromQuery] string showtimeId)
        {
            if (string.IsNullOrEmpty(showtimeId))
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["ShowtimeId"] = showtimeId;
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
        public IActionResult Success([FromQuery] string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderId"] = orderId;
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [Route("Home/Ticket/{orderCode}")]
        public async Task<IActionResult> Ticket(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode)) return Content("<h1 style=\"color:white;text-align:center;font-family:sans-serif;\">Không tìm thấy vé</h1>", "text/html");

            var query = await (from v in _context.Vexemphims
                         join g in _context.Ghengois on v.Maghe equals g.Maghe into vg
                         from g in vg.DefaultIfEmpty()
                         join lc in _context.Lichchieus on v.Malichchieu equals lc.Malichchieu
                         join p in _context.Phims on lc.Maphim equals p.Maphim
                         join pr in _context.Phongrapphims on lc.Maphong equals pr.Maphong
                         join r in _context.Rapphims on pr.Marapphim equals r.Marapphim
                         where v.Madondatve == orderCode
                         select new
                         {
                             maDonDatVe = v.Madondatve,
                             trangthai = v.Trangthai,
                             tenGhe = (g == null ? "" : g.Mahangghe + g.Soghe),
                             tenPhim = p.Tenphim,
                             gioChieu = lc.Giochieu,
                             tenRapPhim = r.Tenrapphim,
                             tenPhong = pr.Tenphong,
                             ngayChieu = lc.Ngaychieu
                         }).ToListAsync();

            if (query == null || !query.Any())
            {
                return Content("<h1 style=\"color:white;text-align:center;font-family:sans-serif;\">Không tìm thấy vé</h1>", "text/html");
            }
            
            var firstTicket = query.First();
            var allSeats = string.Join(", ", query.Select(q => q.tenGhe).OrderBy(s => s));

            ViewData["TenPhim"] = firstTicket.tenPhim;
            ViewData["TenRap"] = firstTicket.tenRapPhim;
            ViewData["NgayChieu"] = firstTicket.ngayChieu.ToString("dd/MM/yyyy");
            ViewData["GioChieu"] = firstTicket.gioChieu.ToString(@"hh\:mm");
            ViewData["TenPhong"] = firstTicket.tenPhong;
            ViewData["TenGhe"] = allSeats;
            ViewData["MaDonDatVe"] = firstTicket.maDonDatVe;

            string statusClass = "status-active";
            string statusText = "VALID";

            if (query.All(q => q.trangthai == "used" || q.trangthai == "USED"))
            {
                statusClass = "status-used";
                statusText = "USED";
            }
            else if (query.All(q => q.trangthai == "expired" || q.trangthai == "EXPIRED"))
            {
                statusClass = "status-expired";
                statusText = "EXPIRED";
            }

            ViewData["StatusClass"] = statusClass;
            ViewData["StatusText"] = statusText;

            return View();
        }
    }
}
