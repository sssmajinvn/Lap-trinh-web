using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("Admin")]
    public class AdminViewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminViewController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("Movies")]
        public IActionResult Movies()
        {
            return View();
        }

        [HttpGet("Showtimes")]
        public IActionResult Showtimes()
        {
            return View();
        }

        // --- Custom endpoints to perform movie updates & deletes without modifying existing backend APIs ---

        [HttpPost("api/movies/update/{id}")]
        public async Task<IActionResult> UpdateMovie(string id, [FromBody] MovieUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { status = "error", message = "Dữ liệu cập nhật không hợp lệ" });

            var movie = await _context.Phims.FirstOrDefaultAsync(p => p.Maphim == id);
            if (movie == null)
                return NotFound(new { status = "error", message = "Không tìm thấy phim cần cập nhật" });

            try
            {
                movie.Tenphim = dto.Tenphim;
                movie.Mota = dto.Mota;
                movie.Thoiluong = dto.Thoiluong;
                movie.Ngayramat = dto.Ngayramat;
                movie.PosterUrl = dto.PosterUrl;
                movie.TrailerUrl = dto.TrailerUrl;
                movie.Gioihantuoi = dto.Gioihantuoi;
                movie.Trangthai = dto.Trangthai;

                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Cập nhật phim thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message });
            }
        }

        [HttpPost("api/movies/delete/{id}")]
        public async Task<IActionResult> DeleteMovie(string id)
        {
            var movie = await _context.Phims
                .Include(p => p.Lichchieus)
                .FirstOrDefaultAsync(p => p.Maphim == id);

            if (movie == null)
                return NotFound(new { status = "error", message = "Không tìm thấy phim cần xóa" });

            try
            {
                // Kiểm tra xem phim có lịch chiếu hoặc vé liên quan không
                var hasTickets = await _context.Vexemphims
                    .AnyAsync(v => v.MalichchieuNavigation!.Maphim == id);

                if (hasTickets)
                {
                    // Nếu đã có vé bán ra, chuyển trạng thái sang "hidden" hoặc "deleted" để ẩn đi thay vì xóa cứng (tránh lỗi khóa ngoại)
                    movie.Trangthai = "hidden";
                    await _context.SaveChangesAsync();
                    return Ok(new { status = "success", message = "Phim đã bán vé nên được ẩn khỏi hệ thống thay vì xóa cứng." });
                }

                // Nếu chưa bán vé, có thể xóa lịch chiếu trống và xóa phim
                if (movie.Lichchieus.Any())
                {
                    _context.Lichchieus.RemoveRange(movie.Lichchieus);
                }

                _context.Phims.Remove(movie);
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Xóa phim thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi khi xóa phim: " + ex.Message });
            }
        }

        // --- Showtime CRUD endpoints (nội bộ MVC, không đụng vào API backend gốc) ---

        [HttpPost("api/showtimes/update/{id}")]
        public async Task<IActionResult> UpdateShowtime(string id, [FromBody] ShowtimeUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { status = "error", message = "Dữ liệu không hợp lệ" });

            var showtime = await _context.Lichchieus.FirstOrDefaultAsync(l => l.Malichchieu == id);
            if (showtime == null)
                return NotFound(new { status = "error", message = "Không tìm thấy lịch chiếu cần cập nhật" });

            try
            {
                showtime.Maphong    = dto.Maphong;
                showtime.Ngaychieu  = dto.Ngaychieu;
                showtime.Giochieu   = dto.Giochieu;
                showtime.Gioketthuc = dto.Gioketthuc;
                showtime.Giave      = dto.Giave;

                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Cập nhật lịch chiếu thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message });
            }
        }

        [HttpPost("api/showtimes/delete/{id}")]
        public async Task<IActionResult> DeleteShowtime(string id)
        {
            var showtime = await _context.Lichchieus
                .Include(l => l.Vexemphims)
                .FirstOrDefaultAsync(l => l.Malichchieu == id);

            if (showtime == null)
                return NotFound(new { status = "error", message = "Không tìm thấy lịch chiếu cần xóa" });

            try
            {
                if (showtime.Vexemphims.Any())
                    return BadRequest(new { status = "error", message = "Không thể xóa lịch chiếu đã có vé được đặt. Hãy hủy các vé trước." });

                _context.Lichchieus.Remove(showtime);
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Xóa lịch chiếu thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi khi xóa lịch chiếu: " + ex.Message });
            }
        }
    }

    public class MovieUpdateDto
    {
        public string Tenphim { get; set; } = null!;
        public DateTime Ngayramat { get; set; }
        public string Mota { get; set; } = null!;
        public int Thoiluong { get; set; }
        public int Gioihantuoi { get; set; }
        public string PosterUrl { get; set; } = null!;
        public string TrailerUrl { get; set; } = null!;
        public string Trangthai { get; set; } = null!;
    }

    public class ShowtimeUpdateDto
    {
        public string Maphong { get; set; } = null!;
        public DateTime Ngaychieu { get; set; }
        public DateTime Giochieu { get; set; }
        public DateTime Gioketthuc { get; set; }
        public int Giave { get; set; }
    }
}
