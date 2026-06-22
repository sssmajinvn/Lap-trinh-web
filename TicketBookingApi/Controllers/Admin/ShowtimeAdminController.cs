using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/showtimes")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class ShowtimeAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ShowtimeAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var showtimes = await _context.Lichchieus
                .Select(s => new {
                    s.Malichchieu,
                    s.Ngaychieu,
                    s.Giochieu,
                    s.Gioketthuc,
                    s.Giave,
                    s.Maphim,
                    s.Maphong
                })
                .ToListAsync();
            return Ok(new { status = "success", data = showtimes });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var showtime = await _context.Lichchieus
                .Where(s => s.Malichchieu == id)
                .Select(s => new {
                    s.Malichchieu,
                    s.Ngaychieu,
                    s.Giochieu,
                    s.Gioketthuc,
                    s.Giave,
                    s.Maphim,
                    s.Maphong
                })
                .FirstOrDefaultAsync();
            if (showtime == null)
                return NotFound(new { status = "error", message = "Showtime not found" });
            return Ok(new { status = "success", data = showtime });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShowtimeDto dto)
        {
            if (dto == null)
                return BadRequest(new { status = "error", message = "Invalid payload" });

            if (string.IsNullOrWhiteSpace(dto.Maphim) || string.IsNullOrWhiteSpace(dto.Maphong)
                || dto.Giave <= 0)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Vui lòng điền đầy đủ thông tin: maPhim, maPhong, ngayChieu, gioChieu, giaVe"
                });
            }

            var phim = await _context.Phims.FirstOrDefaultAsync(p => p.Maphim == dto.Maphim);
            if (phim == null)
                return NotFound(new { status = "error", message = "Không tìm thấy phim với mã: " + dto.Maphim });

            var gioChieu = dto.Giochieu;
            if (gioChieu == default)
                return BadRequest(new { status = "error", message = "Định dạng gioChieu không hợp lệ. Dùng ISO 8601 (VD: 2024-05-15T19:00:00)" });

            var thoiLuong = phim.Thoiluong;
            var gioKetThuc = gioChieu.AddMinutes(thoiLuong + 15);

            var ngayChieu = dto.Ngaychieu == default ? gioChieu.Date : dto.Ngaychieu.Date;

            var overlap = await _context.Lichchieus
                .Where(lc => lc.Maphong == dto.Maphong
                    && lc.Ngaychieu.Date == ngayChieu
                    && gioChieu < lc.Gioketthuc && gioKetThuc > lc.Giochieu)
                .Select(lc => new { lc.Malichchieu, lc.Giochieu, lc.Gioketthuc })
                .FirstOrDefaultAsync();

            if (overlap != null)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Khung giờ này đã có lịch chiếu tại phòng đã chọn. Vui lòng chọn giờ khác.",
                    conflictWith = overlap
                });
            }

            var lichchieu = new Lichchieu
            {
                Malichchieu = string.IsNullOrWhiteSpace(dto.Malichchieu)
                    ? $"LC-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                    : dto.Malichchieu,
                Ngaychieu = ngayChieu,
                Giochieu = gioChieu,
                Gioketthuc = gioKetThuc,
                Giave = dto.Giave,
                Maphim = dto.Maphim,
                Maphong = dto.Maphong
            };

            _context.Lichchieus.Add(lichchieu);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                status = "success",
                message = $"Tạo lịch chiếu thành công. Giờ kết thúc tự động: {gioKetThuc:dd/MM/yyyy HH:mm} (thời lượng {thoiLuong} phút + 15 phút nghỉ)",
                data = new
                {
                    lichchieu.Malichchieu,
                    lichchieu.Ngaychieu,
                    lichchieu.Giochieu,
                    lichchieu.Gioketthuc,
                    lichchieu.Giave,
                    lichchieu.Maphim,
                    lichchieu.Maphong
                }
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ShowtimeDto dto)
        {
            var existing = await _context.Lichchieus.FirstOrDefaultAsync(s => s.Malichchieu == id);
            if (existing == null)
                return NotFound(new { status = "error", message = "Showtime not found" });

            existing.Ngaychieu = dto.Ngaychieu;
            existing.Giochieu = dto.Giochieu;
            existing.Gioketthuc = dto.Gioketthuc;
            existing.Giave = dto.Giave;
            existing.Maphim = dto.Maphim;
            existing.Maphong = dto.Maphong;

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", data = new {
                existing.Malichchieu,
                existing.Ngaychieu,
                existing.Giochieu,
                existing.Gioketthuc,
                existing.Giave,
                existing.Maphim,
                existing.Maphong
            }});
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _context.Lichchieus.FirstOrDefaultAsync(s => s.Malichchieu == id);
            if (existing == null)
                return NotFound(new { status = "error", message = "Showtime not found" });
            _context.Lichchieus.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Deleted successfully" });
        }
    }
}
