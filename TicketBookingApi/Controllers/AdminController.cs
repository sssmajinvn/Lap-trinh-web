using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(Roles = "admin")] // Trong thực tế sẽ bật role này lên
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPut("movies/{id}/status")]
        public async Task<IActionResult> UpdateMovieStatus(string id, [FromBody] UpdateStatusDto dto)
        {
            var movie = await _context.Phims.FindAsync(id);
            if (movie == null) return NotFound(new { status = "error", message = "Không tìm thấy phim" });

            movie.Trangthai = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Cập nhật thành công" });
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
