using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TicketBookingApi.Hubs;
using TicketBookingApi.Models;
using TicketBookingApi.Services;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IVNPayService _vnPayService;
        private readonly IMoMoService _moMoService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly ILogger<PaymentsController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly IPendingOrderMetadataService _pendingOrderService;
        private readonly IMembershipService _membershipService;

        public PaymentsController(IVNPayService vnPayService, IMoMoService moMoService,
            ApplicationDbContext context, IHubContext<SeatHub> hubContext,
            ILogger<PaymentsController> logger, INotificationService notificationService,
            IConfiguration configuration, IPendingOrderMetadataService pendingOrderService,
            IMembershipService membershipService)
        {
            _vnPayService = vnPayService;
            _moMoService = moMoService;
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _notificationService = notificationService;
            _configuration = configuration;
            _pendingOrderService = pendingOrderService;
            _membershipService = membershipService;
        }

        private int? GetUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }

        // ===== VNPay =====

        /// <summary>VNPay redirect người dùng về sau khi thanh toán</summary>
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

            if (!_vnPayService.ValidateSignature(Request.Query))
                return Redirect($"{frontendUrl}/payment/failed?message=InvalidSignature");

            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var orderId = Request.Query["vnp_TxnRef"].ToString();
            var transId = Request.Query["vnp_TransactionNo"].ToString();

            var order = await _context.Dondatves.FirstOrDefaultAsync(d => d.Madondatve == orderId);

            if (responseCode == "00")
            {
                _logger.LogInformation("VNPay Return SUCCESS for order {OrderId}", orderId);

                // Cập nhật DB nếu chưa được xử lý bởi IPN (tránh duplicate)
                if (order != null && order.Trangthai == "pending")
                {
                    await ConfirmPaymentSuccess(order, "vnpay", transId);
                    _logger.LogInformation("VNPay Return: Order {OrderId} confirmed PAID via Return URL", orderId);
                }

                return Redirect($"{frontendUrl}/payment/success?orderId={orderId}");
            }
            else
            {
                // Cập nhật trạng thái thất bại nếu chưa xử lý
                if (order != null && order.Trangthai == "pending")
                {
                    await MarkPaymentFailed(order, transId);
                }
                return Redirect($"{frontendUrl}/payment/failed?orderId={orderId}");
            }
        }

        /// <summary>VNPay IPN — server-to-server callback (quan trọng: cập nhật DB tại đây)</summary>
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VNPayIpn()
        {
            if (!_vnPayService.ValidateSignature(Request.Query))
            {
                _logger.LogWarning("VNPay IPN: Invalid checksum");
                return Ok(new { RspCode = "97", Message = "Invalid Checksum" });
            }

            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var orderId = Request.Query["vnp_TxnRef"].ToString();
            var transId = Request.Query["vnp_TransactionNo"].ToString();
            var amountStr = Request.Query["vnp_Amount"].ToString();

            var order = await _context.Dondatves.FirstOrDefaultAsync(d => d.Madondatve == orderId);
            if (order == null)
                return Ok(new { RspCode = "01", Message = "Order not found" });

            // Tránh xử lý trùng lặp
            if (order.Trangthai == "paid")
                return Ok(new { RspCode = "02", Message = "Order already confirmed" });

            if (responseCode == "00")
            {
                await ConfirmPaymentSuccess(order, "vnpay", transId);
                _logger.LogInformation("VNPay IPN: Order {OrderId} confirmed PAID", orderId);
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                await MarkPaymentFailed(order, transId);
                _logger.LogWarning("VNPay IPN: Order {OrderId} FAILED with code {Code}", orderId, responseCode);
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
        }

        // ===== MoMo =====

        /// <summary>MoMo redirect người dùng về sau khi thanh toán</summary>
        [HttpGet("momo-return")]
        public async Task<IActionResult> MoMoReturn()
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

            if (!_moMoService.ValidateSignature(Request.Query))
                return Redirect($"{frontendUrl}/payment/failed?message=InvalidSignature");

            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();
            var transId = Request.Query["transId"].ToString();

            var order = await _context.Dondatves.FirstOrDefaultAsync(d => d.Madondatve == orderId);

            if (resultCode == "0")
            {
                if (order != null && order.Trangthai == "pending")
                {
                    await ConfirmPaymentSuccess(order, "momo", transId);
                }
                return Redirect($"{frontendUrl}/payment/success?orderId={orderId}");
            }
            else
            {
                if (order != null && order.Trangthai == "pending")
                {
                    await MarkPaymentFailed(order, transId);
                }
                return Redirect($"{frontendUrl}/payment/failed?orderId={orderId}");
            }
        }

        /// <summary>MoMo IPN — server-to-server callback POST</summary>
        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MoMoIpn([FromBody] JsonElement body)
        {
            // Parse body thành IQueryCollection-like dict để validate
            var dict = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            foreach (var prop in body.EnumerateObject())
                dict[prop.Name] = prop.Value.ToString();

            var queryCollection = new QueryCollection(dict);

            if (!_moMoService.ValidateSignature(queryCollection))
            {
                _logger.LogWarning("MoMo IPN: Invalid signature");
                return StatusCode(400, new { message = "Invalid signature" });
            }

            var resultCode = body.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            var orderId = body.TryGetProperty("orderId", out var oid) ? oid.GetString() ?? "" : "";
            var transId = body.TryGetProperty("transId", out var tid) ? tid.ToString() : "";

            var order = await _context.Dondatves.FirstOrDefaultAsync(d => d.Madondatve == orderId);
            if (order == null) return NoContent();

            if (order.Trangthai == "paid") return NoContent(); // Đã xử lý rồi

            if (resultCode == 0)
            {
                await ConfirmPaymentSuccess(order, "momo", transId);
                _logger.LogInformation("MoMo IPN: Order {OrderId} confirmed PAID", orderId);
            }
            else
            {
                await MarkPaymentFailed(order, transId);
                _logger.LogWarning("MoMo IPN: Order {OrderId} FAILED with code {Code}", orderId, resultCode);
            }

            return NoContent();
        }

        // ===== Sinh URL thanh toán =====

        /// <summary>Sinh URL thanh toán riêng lẻ sau khi đã chọn xong bắp nước/voucher</summary>
        [Authorize]
        [HttpPost("create-url")]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentUrlDto request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { status = "error", message = "Token không hợp lệ" });

            var order = await _context.Dondatves.FirstOrDefaultAsync(d => d.Madondatve == request.MaDonDatVe);
            if (order == null) return NotFound(new { status = "error", message = "Không tìm thấy đơn đặt vé" });

            if (order.IdKhach != userId.Value)
                return StatusCode(403, new { status = "error", message = "Bạn không có quyền thanh toán đơn này" });
            
            if (order.Trangthai != "pending")
                return BadRequest(new { status = "error", message = "Đơn đặt vé đã được thanh toán hoặc hủy" });

            // Cập nhật phương thức thanh toán vào Thongtinthanhtoan
            var payment = await _context.Thongtinthanhtoans.FirstOrDefaultAsync(t => t.Madondatve == order.Madondatve && t.Trangthai == "pending");
            if (payment != null)
            {
                payment.Phuongthucthanhtoan = request.PaymentMethod;
                await _context.SaveChangesAsync();
            }

            string? paymentUrl = null;
            if (request.PaymentMethod == "vnpay")
            {
                var ipAddr = Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "127.0.0.1";
                paymentUrl = _vnPayService.CreatePaymentUrl(order.Madondatve, (decimal)order.Tongtien, $"Thanh toan ve xem phim DON {order.Madondatve}", ipAddr);
            }
            else if (request.PaymentMethod == "momo")
            {
                paymentUrl = await _moMoService.CreatePaymentUrl(order.Madondatve, (decimal)order.Tongtien, $"Thanh toan ve xem phim DON {order.Madondatve}");
            }

            if (paymentUrl == null)
                return BadRequest(new { status = "error", message = "Phương thức thanh toán không hợp lệ" });

            return Ok(new { status = "success", data = new { paymentUrl } });
        }

        // ===== Helper methods =====

        private async Task ConfirmPaymentSuccess(Dondatve order, string method, string transId)
        {
            List<string> seatIds = new List<string>();
            string? showtimeId = null;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                order.Trangthai = "paid";

                // Cập nhật thongtinthanhtoan
                var payment = await _context.Thongtinthanhtoans
                    .FirstOrDefaultAsync(t => t.Madondatve == order.Madondatve && t.Trangthai == "pending");
                if (payment != null)
                {
                    payment.Trangthai = "success";
                    payment.Paymentgatewaytransactionid = transId;
                    payment.Thoidiemthanhtoan = DateTime.UtcNow;
                }

                // Kích hoạt tất cả vé
                var tickets = await _context.Vexemphims
                    .Where(v => v.Madondatve == order.Madondatve)
                    .ToListAsync();
                tickets.ForEach(t => t.Trangthai = "active");

                seatIds = tickets.Select(t => t.Maghe).ToList();
                showtimeId = tickets.FirstOrDefault()?.Malichchieu;

                // Ghi nhận voucher khi thanh toán thành công (không ghi lúc giữ ghế)
                var meta = _pendingOrderService.Get(order.Madondatve);
                if (meta?.VoucherId != null)
                {
                    _context.LichSuKhuyenMais.Add(new LichSuKhuyenMai
                    {
                        IdKhuyenMai = meta.VoucherId.Value,
                        IdKhach = order.IdKhach,
                        Madondatve = order.Madondatve,
                        GiaTriGiamThucTe = meta.VoucherDiscountAmount,
                        NgaySuDung = DateTime.UtcNow
                    });

                    var voucher = await _context.KhuyenMais.FindAsync(meta.VoucherId.Value);
                    if (voucher != null)
                        voucher.SoLuongDaDung = (voucher.SoLuongDaDung ?? 0) + 1;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Cộng chi tiêu và nâng hạng thành viên
                await _membershipService.ApplyPaymentSpendingAsync(order.IdKhach, order.Tongtien);
                _pendingOrderService.Remove(order.Madondatve);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            // --- Thực hiện ngoài Transaction ---
            try
            {
                if (showtimeId != null)
                {
                    await _hubContext.Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new
                    {
                        seatIds,
                        status = "booked"
                    });
                }

                // Gửi thông báo (Maphim được set null vì truyền showtimeId sẽ bị lỗi Foreign Key)
                await _notificationService.CreateAndSendNotificationAsync(order.IdKhach, "Thanh toán thành công", $"Đơn hàng {order.Madondatve} đã được thanh toán thành công. Chúc bạn xem phim vui vẻ!", order.Madondatve, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo/SignalR sau khi thanh toán đơn {OrderId}", order.Madondatve);
            }
        }

        private async Task MarkPaymentFailed(Dondatve order, string transId)
        {
            order.Trangthai = "cancelled";
            var payment = await _context.Thongtinthanhtoans
                .FirstOrDefaultAsync(t => t.Madondatve == order.Madondatve && t.Trangthai == "pending");
            if (payment != null)
            {
                payment.Trangthai = "failed";
                payment.Paymentgatewaytransactionid = transId;
            }
            var tickets = await _context.Vexemphims
                .Where(v => v.Madondatve == order.Madondatve)
                .ToListAsync();
            tickets.ForEach(t => t.Trangthai = "cancelled");
            await _context.SaveChangesAsync();

            _pendingOrderService.Remove(order.Madondatve);

            // Broadcast SignalR để nhả ghế cho các client khác
            var seatIds = tickets.Select(t => t.Maghe).ToList();
            var showtimeId = tickets.FirstOrDefault()?.Malichchieu;

            try
            {
                if (showtimeId != null && seatIds.Any())
                {
                    await _hubContext.Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new
                    {
                        seatIds,
                        status = "available"
                    });
                }

                // Gửi thông báo (Maphim = null)
                await _notificationService.CreateAndSendNotificationAsync(order.IdKhach, "Thanh toán thất bại", $"Đơn hàng {order.Madondatve} đã bị hủy do thanh toán lỗi hoặc người dùng chủ động hủy giao dịch.", order.Madondatve, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo/SignalR sau khi hủy đơn {OrderId}", order.Madondatve);
            }
        }
    }

    public class CreatePaymentUrlDto
    {
        public string MaDonDatVe { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "vnpay"; // vnpay hoặc momo
    }
}

