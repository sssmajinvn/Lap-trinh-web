using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Thongtintaikhoan> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<Thongtintaikhoan> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private int GetUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(raw, out var id) || id == 0)
                return -1;
            return id;
        }

        // GET /api/users/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId < 0)
                return Unauthorized(new { status = "error", message = "Token không hợp lệ" });
            var user = await _context.Thongtintaikhoans
                .Where(u => u.IdKhach == userId)
                .Select(u => new
                {
                    u.IdKhach,
                    u.Mataikhoan,
                    u.Hoten,
                    u.Ngaysinh,
                    u.Gioitinh,
                    u.Sdt,
                    u.Email,
                    u.Anhdaidien,
                    u.Ngaytao,
                    u.HangThanhVien,
                    u.TongChiTieu
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });

            return Ok(new { status = "success", data = user });
        }

        // PUT /api/users/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            if (userId < 0)
                return Unauthorized(new { status = "error", message = "Token không hợp lệ" });
            var user = await _context.Thongtintaikhoans.FindAsync(userId);

            if (user == null)
                return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });

            var isGoogleUser = user.Mataikhoan.StartsWith("GG", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(dto.HoTen)) user.Hoten = dto.HoTen;
            if (dto.NgaySinh.HasValue) user.Ngaysinh = dto.NgaySinh.Value;
            if (dto.GioiTinh.HasValue) user.Gioitinh = dto.GioiTinh.Value;
            if (!string.IsNullOrWhiteSpace(dto.AnhDaiDien)) user.Anhdaidien = dto.AnhDaiDien;
            if (!string.IsNullOrWhiteSpace(dto.Sdt)) user.Sdt = dto.Sdt;

            // Không cho phép tài khoản Google đổi email
            if (!isGoogleUser && !string.IsNullOrWhiteSpace(dto.Email))
                user.Email = dto.Email;

            user.Ngaycapnhat = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "success",
                message = "Cập nhật thông tin thành công",
                data = new { user.IdKhach, user.Mataikhoan, user.Hoten, user.Email, user.Sdt, user.Anhdaidien }
            });
        }

        // PUT /api/users/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserId();
            if (userId < 0)
                return Unauthorized(new { status = "error", message = "Token không hợp lệ" });
            var user = await _context.Thongtintaikhoans.FindAsync(userId);

            if (user == null)
                return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });

            if (user.Mataikhoan.StartsWith("GG", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { status = "error", message = "Tài khoản Google không hỗ trợ đổi mật khẩu theo cách này" });

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { status = "error", message = "Mật khẩu cũ không chính xác", errors = result.Errors });

            return Ok(new { status = "success", message = "Đổi mật khẩu thành công" });
        }

        // GET /api/users/booking-history?page=1&limit=10&status=paid
        [HttpGet("booking-history")]
        public async Task<IActionResult> GetBookingHistory(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? status = null)
        {
            var userId = GetUserId();
            if (userId < 0)
                return Unauthorized(new { status = "error", message = "Token không hợp lệ" });
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 50);

            var query = _context.Dondatves
                .Where(d => d.IdKhach == userId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusList = status.Split(',').Select(s => s.Trim()).ToList();
                query = query.Where(d => statusList.Contains(d.Trangthai));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / limit);

            var orders = await query
                .OrderByDescending(d => d.Ngaydatve)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(d => d.Vexemphims)
                    .ThenInclude(v => v.MalichchieuNavigation)
                        .ThenInclude(l => l!.MaphimNavigation)
                .Include(d => d.Vexemphims)
                    .ThenInclude(v => v.MalichchieuNavigation)
                        .ThenInclude(l => l!.MaphongNavigation)
                            .ThenInclude(p => p!.MarapphimNavigation)
                .Include(d => d.Vexemphims)
                    .ThenInclude(v => v.MagheNavigation)
                .ToListAsync();

            var history = orders.Select(d =>
            {
                var firstTicket = d.Vexemphims.FirstOrDefault();
                var showtime = firstTicket?.MalichchieuNavigation;
                var phong = showtime?.MaphongNavigation;
                var rap = phong?.MarapphimNavigation;
                var phim = showtime?.MaphimNavigation;

                return new
                {
                    maDonDatVe = d.Madondatve,
                    tongTien = d.Tongtien,
                    trangThai = d.Trangthai,
                    ngayDatVe = d.Ngaydatve,
                    tenPhim = phim?.Tenphim,
                    posterUrl = phim?.PosterUrl,
                    tenRapPhim = rap?.Tenrapphim,
                    diaChi = rap?.Diachi,
                    tenPhong = phong?.Tenphong,
                    ngayChieu = showtime?.Ngaychieu.ToString("yyyy-MM-dd"),
                    gioChieu = showtime?.Giochieu.ToString("HH:mm"),
                    tickets = d.Vexemphims.Select(v => new
                    {
                        maVe = v.Mavexemphim,
                        giaVe = v.Giave,
                        maGhe = v.Maghe,
                        tenGhe = v.MagheNavigation != null
                            ? v.MagheNavigation.Mahangghe + v.MagheNavigation.Soghe
                            : v.Maghe,
                        trangThai = v.Trangthai,
                        qrCode = v.Qrcode
                    }).ToList()
                };
            });

            return Ok(new
            {
                status = "success",
                data = history,
                pagination = new { total, page, limit, totalPages, hasMore = page < totalPages }
            });
        }
    }

    public class UpdateProfileDto
    {
        public string? HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public short? GioiTinh { get; set; }
        public string? AnhDaiDien { get; set; }
        public string? Sdt { get; set; }
        public string? Email { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
