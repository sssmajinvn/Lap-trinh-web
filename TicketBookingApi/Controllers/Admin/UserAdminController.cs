using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class UserAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Thongtintaikhoans.ToListAsync();
            return Ok(new { status = "success", data = users });
        }

        // GET: api/admin/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.IdKhach == id);
            if (user == null)
                return NotFound(new { status = "error", message = "User not found" });
            return Ok(new { status = "success", data = user });
        }

        // PUT: api/admin/users/{id}
        // Update user information (e.g., HangThanhVien, Trangthai)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.IdKhach == id);
            if (user == null)
                return NotFound(new { status = "error", message = "User not found" });

            // Apply allowed updates
            if (dto.HangThanhVien != null) user.HangThanhVien = dto.HangThanhVien;
            if (dto.Trangthai != null) user.Trangthai = dto.Trangthai;
            if (dto.TongChiTieu.HasValue) user.TongChiTieu = dto.TongChiTieu.Value;

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", data = user });
        }

        // PATCH: api/admin/users/{id}/deactivate
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.IdKhach == id);
            if (user == null)
                return NotFound(new { status = "error", message = "User not found" });

            user.Trangthai = "inactive"; // or any convention used in the project
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "User deactivated", data = user });
        }
    }
}
