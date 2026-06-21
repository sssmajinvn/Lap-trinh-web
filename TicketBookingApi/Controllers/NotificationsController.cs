using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : 0;
        }

        // GET /api/notifications
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetUserId();

            var notifications = await _context.Thongbaos
                .Where(tb => tb.IdKhach == userId)
                .OrderByDescending(tb => tb.Thoidiemtb)
                .Include(tb => tb.MaphimNavigation)
                .Select(tb => new
                {
                    maThongBao = tb.Mathongbao,
                    tieuDe = tb.Tieude,
                    noiDung = tb.Noidung,
                    trangThai = tb.Trangthai,
                    thoiDiemTB = tb.Thoidiemtb,
                    thoiDiemXem = tb.Thoidiemxem,
                    maDonDatVe = tb.Madondatve,
                    phim = tb.MaphimNavigation != null ? new
                    {
                        maPhim = tb.MaphimNavigation.Maphim,
                        tenPhim = tb.MaphimNavigation.Tenphim,
                        posterUrl = tb.MaphimNavigation.PosterUrl
                    } : null
                })
                .ToListAsync();

            return Ok(new { status = "success", data = notifications });
        }

        // GET /api/notifications/unread-count
        [Authorize]
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();

            var count = await _context.Thongbaos
                .CountAsync(tb => tb.IdKhach == userId && tb.Trangthai == "unread");

            return Ok(new { status = "success", data = new { unreadCount = count } });
        }

        // PUT /api/notifications/{id}/read
        [Authorize]
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var userId = GetUserId();

            var notification = await _context.Thongbaos
                .FirstOrDefaultAsync(tb => tb.Mathongbao == id && tb.IdKhach == userId);

            if (notification == null)
                return NotFound(new { status = "error", message = "Không tìm thấy thông báo hoặc không có quyền thực hiện" });

            notification.Trangthai = "read";
            notification.Thoidiemxem = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Đã đánh dấu đã đọc" });
        }

        // PUT /api/notifications/read-all
        [Authorize]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();

            var unread = await _context.Thongbaos
                .Where(tb => tb.IdKhach == userId && tb.Trangthai == "unread")
                .ToListAsync();

            foreach (var tb in unread)
            {
                tb.Trangthai = "read";
                tb.Thoidiemxem = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Đã đánh dấu tất cả thông báo là đã đọc" });
        }

        // DELETE /api/notifications/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(string id)
        {
            var userId = GetUserId();

            var notification = await _context.Thongbaos
                .FirstOrDefaultAsync(tb => tb.Mathongbao == id && tb.IdKhach == userId);

            if (notification == null)
                return NotFound(new { status = "error", message = "Không tìm thấy thông báo hoặc không có quyền xóa" });

            _context.Thongbaos.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Đã xóa thông báo thành công" });
        }

        // DELETE /api/notifications
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var userId = GetUserId();

            var notifications = await _context.Thongbaos
                .Where(tb => tb.IdKhach == userId)
                .ToListAsync();

            _context.Thongbaos.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Đã xóa tất cả thông báo thành công" });
        }
    }
}
