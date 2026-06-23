using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/theaters")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class TheaterAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TheaterAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await _context.Phongrapphims
                .Include(r => r.MarapphimNavigation)
                .Select(r => new
                {
                    maphong = r.Maphong,
                    tenphong = r.Tenphong,
                    soluongghe = r.Soluongghe,
                    marapphim = r.Marapphim,
                    tenrapphim = r.MarapphimNavigation.Tenrapphim
                })
                .ToListAsync();

            return Ok(new { status = "success", data = rooms });
        }
    }
}
