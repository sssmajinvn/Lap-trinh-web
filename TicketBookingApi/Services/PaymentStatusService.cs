using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Hubs;
using TicketBookingApi.Models;

namespace TicketBookingApi.Services
{
    public class PaymentStatusResult
    {
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string RequestedStatus { get; set; } = string.Empty;
    }

    public interface IPaymentStatusService
    {
        Task<PaymentStatusResult> UpdatePaymentStatusAsync(string paymentId, string requestedStatus, string? transactionId = null);
    }

    public class PaymentStatusService : IPaymentStatusService
    {
        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "success", "failed", "pending", "cancelled"
        };

        private readonly ApplicationDbContext _context;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentStatusService> _logger;

        public PaymentStatusService(
            ApplicationDbContext context,
            IHubContext<SeatHub> hubContext,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<PaymentStatusService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PaymentStatusResult> UpdatePaymentStatusAsync(string paymentId, string requestedStatus, string? transactionId = null)
        {
            if (!ValidStatuses.Contains(requestedStatus))
            {
                throw new InvalidOperationException($"Invalid payment status: {requestedStatus}");
            }

            var payment = await _context.Thongtinthanhtoans
                .Include(t => t.MadondatveNavigation)
                    .ThenInclude(d => d.Vexemphims)
                .Include(t => t.MadondatveNavigation)
                    .ThenInclude(d => d.IdKhachNavigation)
                .FirstOrDefaultAsync(t => t.Mathanhtoan == paymentId);

            if (payment == null)
            {
                throw new KeyNotFoundException($"Payment not found: {paymentId}");
            }

            var order = payment.MadondatveNavigation;
            var tickets = order.Vexemphims.ToList();
            var seatIds = tickets.Select(t => t.Maghe).ToList();
            var showtimeId = tickets.FirstOrDefault()?.Malichchieu;
            var normalizedRequestedStatus = requestedStatus.ToLowerInvariant();
            var dbPaymentStatus = normalizedRequestedStatus == "cancelled" ? "failed" : normalizedRequestedStatus;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                payment.Trangthai = dbPaymentStatus;
                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    payment.Paymentgatewaytransactionid = transactionId;
                }

                if (normalizedRequestedStatus == "success")
                {
                    payment.Thoidiemthanhtoan = DateTime.UtcNow;
                    order.Trangthai = "paid";
                    foreach (var ticket in tickets)
                    {
                        ticket.Trangthai = "active";
                    }
                }
                else if (normalizedRequestedStatus == "failed" || normalizedRequestedStatus == "cancelled")
                {
                    order.Trangthai = "cancelled";
                    foreach (var ticket in tickets)
                    {
                        ticket.Trangthai = "cancelled";
                    }
                }
                else
                {
                    order.Trangthai = "pending";
                    foreach (var ticket in tickets)
                    {
                        ticket.Trangthai = "pending";
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(showtimeId) && seatIds.Count > 0)
                {
                    var seatStatus = normalizedRequestedStatus == "success"
                        ? "booked"
                        : normalizedRequestedStatus == "pending"
                            ? "pending"
                            : "available";

                    await _hubContext.Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new
                    {
                        seatIds,
                        status = seatStatus
                    });
                }

                if (normalizedRequestedStatus == "failed" || normalizedRequestedStatus == "cancelled")
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        order.IdKhach,
                        "Đơn hàng đã bị hủy",
                        $"Đơn hàng {order.Madondatve} đã bị hủy bởi quản trị viên hoặc do cập nhật trạng thái thanh toán.",
                        order.Madondatve,
                        null);

                    await _emailService.SendBookingCancelledEmailAsync(order.Madondatve);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Post-payment status side effects failed for {PaymentId}", paymentId);
            }

            return new PaymentStatusResult
            {
                PaymentStatus = payment.Trangthai,
                OrderStatus = order.Trangthai,
                RequestedStatus = normalizedRequestedStatus
            };
        }
    }
}
