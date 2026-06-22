using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/admin/showtimes
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

        // GET: api/admin/showtimes/{id}
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

        // POST: api/admin/showtimes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShowtimeDto dto)
        {
            if (dto == null)
                return BadRequest(new { status = "error", message = "Invalid payload" });

            var lichchieu = new Lichchieu
            {
                Malichchieu = string.IsNullOrWhiteSpace(dto.Malichchieu)
                    ? $"LC-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                    : dto.Malichchieu,
                Ngaychieu = dto.Ngaychieu,
                Giochieu = dto.Giochieu,
                Gioketthuc = dto.Gioketthuc,
                Giave = dto.Giave,
                Maphim = dto.Maphim,
                Maphong = dto.Maphong
            };

            _context.Lichchieus.Add(lichchieu);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = lichchieu.Malichchieu },
                new { status = "success", data = new {
                    lichchieu.Malichchieu,
                    lichchieu.Ngaychieu,
                    lichchieu.Giochieu,
                    lichchieu.Gioketthuc,
                    lichchieu.Giave,
                    lichchieu.Maphim,
                    lichchieu.Maphong
                }});
        }

        // PUT: api/admin/showtimes/{id}
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

        // DELETE: api/admin/showtimes/{id}
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
