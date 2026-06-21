using Microsoft.AspNetCore.SignalR;
using TicketBookingApi.Hubs;
using TicketBookingApi.Models;

namespace TicketBookingApi.Services
{
    public interface INotificationService
    {
        Task CreateAndSendNotificationAsync(int userId, string title, string content, string? maDonDatVe = null, string? maphim = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAndSendNotificationAsync(int userId, string title, string content, string? maDonDatVe = null, string? maphim = null)
        {
            var mathongbao = "TB" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            
            var notification = new Thongbao
            {
                Mathongbao = mathongbao,
                IdKhach = userId,
                Tieude = title,
                Noidung = content,
                Trangthai = "unread",
                Ngaytao = DateTime.UtcNow,
                Thoidiemtb = DateTime.UtcNow,
                Madondatve = string.IsNullOrEmpty(maDonDatVe) ? null : maDonDatVe,
                Maphim = string.IsNullOrEmpty(maphim) ? null : maphim
            };

            _context.Thongbaos.Add(notification);
            await _context.SaveChangesAsync();

            // Phát sự kiện qua SignalR đến user cụ thể
            // Mặc định SignalR sử dụng ClaimTypes.NameIdentifier làm UserIdentifier
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                maThongBao = notification.Mathongbao,
                tieuDe = notification.Tieude,
                noiDung = notification.Noidung,
                trangThai = notification.Trangthai,
                thoiDiemTB = notification.Thoidiemtb,
                maDonDatVe = notification.Madondatve,
                maphim = notification.Maphim
            });
        }
    }
}
