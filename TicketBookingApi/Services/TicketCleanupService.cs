using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using Microsoft.AspNetCore.SignalR;

namespace TicketBookingApi.Services
{
    public class TicketCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TicketCleanupService> _logger;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.SeatHub> _hubContext;

        public TicketCleanupService(IServiceProvider serviceProvider, ILogger<TicketCleanupService> logger, Microsoft.AspNetCore.SignalR.IHubContext<Hubs.SeatHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TicketCleanupService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupPendingTickets();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing TicketCleanupService.");
                }

                // Chạy mỗi phút 1 lần
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("TicketCleanupService is stopping.");
        }

        private async Task CleanupPendingTickets()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var pendingOrderService = scope.ServiceProvider.GetRequiredService<IPendingOrderMetadataService>();

            var expirationTime = DateTime.UtcNow.AddMinutes(-5);

            // Tìm các đơn hàng pending đã quá hạn 5 phút (dựa vào thoidiemthanhtoan hoặc thông tin liên quan)
            // Lấy các thongtinthanhtoan pending quá 5 phút
            var expiredPayments = await context.Thongtinthanhtoans
                .Where(t => t.Trangthai == "pending" && t.Thoidiemthanhtoan < expirationTime)
                .ToListAsync();

            if (expiredPayments.Any())
            {
                var notificationsToSend = new List<(int UserId, string MaDonDatVe)>();

                foreach (var payment in expiredPayments)
                {
                    payment.Trangthai = "failed";
                    
                    var order = await context.Dondatves.FirstOrDefaultAsync(d => d.Madondatve == payment.Madondatve);
                    if (order != null && order.Trangthai == "pending")
                    {
                        order.Trangthai = "cancelled";
                    }

                    var tickets = await context.Vexemphims.Where(v => v.Madondatve == payment.Madondatve).ToListAsync();
                    foreach (var ticket in tickets)
                    {
                        ticket.Trangthai = "cancelled";
                    }

                    var seatIds = tickets.Select(t => t.Maghe).ToList();
                    var showtimeId = tickets.FirstOrDefault()?.Malichchieu;
                    if (showtimeId != null && seatIds.Any())
                    {
                        await _hubContext.Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new
                        {
                            seatIds,
                            status = "available"
                        });
                    }

                    if (order != null)
                    {
                        pendingOrderService.Remove(order.Madondatve);
                        notificationsToSend.Add((order.IdKhach, order.Madondatve));
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired pending bookings.", expiredPayments.Count);

                foreach (var (userId, maDonDatVe) in notificationsToSend)
                {
                    try
                    {
                        await notificationService.CreateAndSendNotificationAsync(
                            userId,
                            "Đơn hàng hết hạn",
                            $"Đơn hàng {maDonDatVe} đã tự động bị hủy do quá thời gian giữ ghế 5 phút.",
                            maDonDatVe);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi thông báo hết hạn đơn hàng {OrderId}", maDonDatVe);
                    }
                }
            }
        }
    }
}
