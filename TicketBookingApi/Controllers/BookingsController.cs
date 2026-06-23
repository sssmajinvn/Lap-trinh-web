using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Hubs;
using TicketBookingApi.Models;
using TicketBookingApi.Services;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVNPayService _vnPayService;
        private readonly IMoMoService _moMoService;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly TemporarySeatLockService _lockService;
        private readonly INotificationService _notificationService;
        private readonly IVoucherService _voucherService;
        private readonly IPendingOrderMetadataService _pendingOrderService;

        public BookingsController(ApplicationDbContext context, IVNPayService vnPayService, IMoMoService moMoService, IHubContext<SeatHub> hubContext, TemporarySeatLockService lockService, INotificationService notificationService, IVoucherService voucherService, IPendingOrderMetadataService pendingOrderService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _moMoService = moMoService;
            _hubContext = hubContext;
            _lockService = lockService;
            _notificationService = notificationService;
            _voucherService = voucherService;
            _pendingOrderService = pendingOrderService;
        }

        // GET /api/bookings/theaters
        [HttpGet("theaters")]
        public async Task<IActionResult> GetTheaters()
        {
            var theaters = await _context.Rapphims.ToListAsync();
            return Ok(new { status = "success", data = theaters });
        }

        // GET /api/bookings/showtimes?movieId=&date=&theaterId=
        [HttpGet("showtimes")]
        public async Task<IActionResult> GetShowtimes([FromQuery] string? movieId, [FromQuery] string? date, [FromQuery] string? theaterId)
        {
            var query = _context.Lichchieus
                .Include(l => l.MaphimNavigation)
                .Include(l => l.MaphongNavigation)
                    .ThenInclude(p => p!.MarapphimNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(movieId))
                query = query.Where(l => l.Maphim == movieId);

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
                query = query.Where(l => l.Ngaychieu.Date == parsedDate.Date);

            if (!string.IsNullOrEmpty(theaterId))
                query = query.Where(l => l.MaphongNavigation!.Marapphim == theaterId);

            var showtimes = await query
                .OrderBy(l => l.Ngaychieu).ThenBy(l => l.Giochieu)
                .ToListAsync();

            var data = showtimes.Select(l => new
            {
                l.Malichchieu,
                l.Maphim,
                Tenphim = l.MaphimNavigation?.Tenphim,
                Ngaychieu = l.Ngaychieu.ToString("yyyy-MM-dd"),
                Giochieu = l.Giochieu.ToString("HH:mm"),
                Gioketthuc = l.Gioketthuc.ToString("HH:mm"),
                l.Giave,
                Phong = l.MaphongNavigation?.Tenphong,
                Rap = l.MaphongNavigation?.MarapphimNavigation?.Tenrapphim
            });

            return Ok(new { status = "success", data });
        }

        // GET /api/bookings/showtimes/{showtimeId}/seats
        [HttpGet("showtimes/{showtimeId}/seats")]
        public async Task<IActionResult> GetSeats(string showtimeId)
        {
            var showtime = await _context.Lichchieus
                .Include(l => l.MaphimNavigation)
                .Include(l => l.MaphongNavigation)
                    .ThenInclude(p => p!.Ghengois)
                .Include(l => l.MaphongNavigation!.MarapphimNavigation)
                .FirstOrDefaultAsync(l => l.Malichchieu == showtimeId);

            if (showtime == null)
                return NotFound(new { status = "error", message = "Không tìm thấy lịch chiếu" });

            // 1. Ghế đã bán hoặc đang chờ thanh toán (màu xám/đã bán)
            var bookedSeats = await _context.Vexemphims
                .Where(v => v.Malichchieu == showtimeId && (v.Trangthai == "active" || v.Trangthai == "pending"))
                .Select(v => v.Maghe)
                .ToListAsync();

            // 2. Ghế đang bị người khác click chọn (màu cam/giữ ghế tạm)
            var tempLockedSeats = _lockService.GetLockedSeatsForShowtime(showtimeId);

            var data = new
            {
                showtime?.Malichchieu,
                tenphim = showtime.MaphimNavigation?.Tenphim,
                Ngaychieu = showtime?.Ngaychieu.ToString("yyyy-MM-dd"),
                Giochieu = showtime?.Giochieu.ToString("HH:mm"),
                showtime?.Giave,
                Phong = showtime?.MaphongNavigation?.Tenphong,
                Rap = showtime?.MaphongNavigation?.MarapphimNavigation?.Tenrapphim,
                Ghe = showtime?.MaphongNavigation?.Ghengois.Select(g => new
                {
                    g.Maghe,
                    g.Mahangghe,
                    g.Soghe,
                    g.Loaighe,
                    g.Hesogiaghe,
                    GiaVe = (int)(showtime.Giave * g.Hesogiaghe),
                    Status = bookedSeats.Contains(g.Maghe) ? "booked" : 
                             tempLockedSeats.Contains(g.Maghe) ? "locked" : "available"
                }).OrderBy(g => g.Mahangghe).ThenBy(g => g.Soghe)
            };

            return Ok(new { status = "success", data });
        }

        // POST /api/bookings/book  (Yêu cầu đăng nhập)
        [Authorize]
        [HttpPost("book")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
        {
            // 1. Lấy userId từ JWT claim
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { status = "error", message = "Token không hợp lệ" });

            if (request.SeatIds == null || request.SeatIds.Count == 0)
                return BadRequest(new { status = "error", message = "Vui lòng chọn ít nhất 1 ghế" });

            if (request.SeatIds.Count > 6)
                return BadRequest(new { status = "error", message = "Bạn chỉ được chọn tối đa 6 ghế" });

            // 2. Lấy thông tin lịch chiếu
            var showtime = await _context.Lichchieus
                .Include(l => l.MaphongNavigation)
                    .ThenInclude(p => p!.MarapphimNavigation)
                .FirstOrDefaultAsync(l => l.Malichchieu == request.ShowtimeId);

            if (showtime == null)
                return NotFound(new { status = "error", message = "Không tìm thấy suất chiếu" });

            // 3. Kiểm tra ghế có còn trống không (double check)
            var alreadyBooked = await _context.Vexemphims
                .Where(v => v.Malichchieu == request.ShowtimeId
                         && request.SeatIds.Contains(v.Maghe)
                         && (v.Trangthai == "active" || v.Trangthai == "pending"))
                .Select(v => v.Maghe)
                .ToListAsync();

            if (alreadyBooked.Any())
                return BadRequest(new { status = "error", message = $"Ghế {string.Join(", ", alreadyBooked)} đã được đặt. Vui lòng chọn ghế khác!" });

            // 4. Tính tiền vé
            var seats = await _context.Ghengois
                .Where(g => request.SeatIds.Contains(g.Maghe))
                .ToListAsync();

            if (seats.Count != request.SeatIds.Count)
                return BadRequest(new { status = "error", message = "Một hoặc nhiều ghế không hợp lệ" });

            var wrongRoomSeats = seats.Where(s => s.Maphong != showtime.Maphong).Select(s => s.Maghe).ToList();
            if (wrongRoomSeats.Any())
                return BadRequest(new { status = "error", message = $"Ghế {string.Join(", ", wrongRoomSeats)} không thuộc phòng chiếu này" });

            int subtotal = 0;
            var seatPrices = new Dictionary<string, int>();
            foreach (var seat in seats)
            {
                var price = (int)(showtime.Giave * seat.Hesogiaghe);
                seatPrices[seat.Maghe] = price;
                subtotal += price;
            }

            // Tính tiền bắp nước
            var orderConcessions = new List<OrderConcession>();
            if (request.Concessions != null && request.Concessions.Any())
            {
                foreach (var conc in request.Concessions)
                {
                    if (conc.Quantity <= 0) continue;

                    int price = 0;
                    if (conc.Type == "item")
                    {
                        var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemId == conc.Id);
                        if (item != null)
                        {
                            price = (int)item.Price;
                            orderConcessions.Add(new OrderConcession
                            {
                                ItemId = item.ItemId,
                                Quantity = conc.Quantity,
                                UnitPrice = price,
                                ThanhTien = price * conc.Quantity
                            });
                        }
                    }
                    else if (conc.Type == "combo")
                    {
                        var combo = await _context.Combos.FirstOrDefaultAsync(c => c.ComboId == conc.Id);
                        if (combo != null)
                        {
                            price = (int)combo.Price;
                            orderConcessions.Add(new OrderConcession
                            {
                                ComboId = combo.ComboId,
                                Quantity = conc.Quantity,
                                UnitPrice = price,
                                ThanhTien = price * conc.Quantity
                            });
                        }
                    }
                    subtotal += (price * conc.Quantity);
                }
            }

            // Tính giảm giá (Rank + Voucher)
            var discountResult = await _voucherService.CalculateDiscountAsync(userId, subtotal, request.MaVoucher, request.ShowtimeId);
            if (!discountResult.IsSuccess)
                return BadRequest(new { status = "error", message = discountResult.ErrorMessage });

            int finalPrice = (int)discountResult.FinalPrice;

            // 5. Sinh mã đơn hàng
            var maDonDatVe = "DON" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(7);
            var maThanhToan = "PAY" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(7);

            // 6. Tạo đơn hàng trong DB (transaction)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Insert đơn đặt vé (pending)
                var donDatVe = new Dondatve
                {
                    Madondatve = maDonDatVe,
                    Tongtien = finalPrice,
                    Trangthai = "pending",
                    IdKhach = userId
                };
                _context.Dondatves.Add(donDatVe);

                // Insert thông tin thanh toán (pending)
                var thanhToan = new Thongtinthanhtoan
                {
                    Mathanhtoan = maThanhToan,
                    Phuongthucthanhtoan = request.PaymentMethod,
                    Sotienthanhtoan = finalPrice,
                    Trangthai = "pending",
                    Madondatve = maDonDatVe,
                    Thoidiemthanhtoan = DateTime.UtcNow
                };
                _context.Thongtinthanhtoans.Add(thanhToan);

                // Insert Bắp nước
                foreach (var oc in orderConcessions)
                {
                    oc.Madondatve = maDonDatVe;
                    _context.OrderConcessions.Add(oc);
                }

                // Insert vé (pending) cho từng ghế
                for (int i = 0; i < request.SeatIds.Count; i++)
                {
                    var seatId = request.SeatIds[i];
                    
                    // Giải phóng khóa tạm thời trên SignalR (nếu có) để nhường quyền ưu tiên cho DB
                    _lockService.UnlockSeat(request.ShowtimeId, seatId, string.Empty);

                    var maVe = "VE" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(7) + i;
                    var expireTime = showtime.Giochieu.AddHours(3);
                    var qrData = $"/ticket/{maVe}";

                    _context.Vexemphims.Add(new Vexemphim
                    {
                        Mavexemphim = maVe,
                        Qrcode = qrData,
                        Giave = seatPrices[seatId],
                        Maghe = seatId,
                        Malichchieu = request.ShowtimeId,
                        Madondatve = maDonDatVe,
                        Trangthai = "pending",
                        Thoigianphathanh = DateTime.UtcNow,
                        Thoigianhethan = expireTime
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Lưu metadata voucher — chỉ ghi DB khi thanh toán thành công
                _pendingOrderService.Set(maDonDatVe, new PendingOrderMetadata
                {
                    UserId = userId,
                    VoucherId = discountResult.AppliedVoucher?.Id,
                    VoucherDiscountAmount = discountResult.VoucherDiscountAmount,
                    RankDiscountAmount = discountResult.RankDiscountAmount
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { status = "error", message = "Lỗi tạo đơn hàng" });
            }

            // 7. Broadcast trạng thái ghế đang chờ thanh toán qua SignalR
            await _hubContext.Clients.Group(request.ShowtimeId).SendAsync("ReceiveSeatStatus", new
            {
                seatIds = request.SeatIds,
                status = "pending"
            });

            // Gửi thông báo
            await _notificationService.CreateAndSendNotificationAsync(userId, "Giữ ghế thành công", $"Vui lòng hoàn tất thanh toán cho đơn hàng {maDonDatVe} trong vòng 5 phút.", maDonDatVe, showtime.Maphim);

            return StatusCode(201, new
            {
                status = "success",
                message = "Giữ ghế thành công! Vui lòng hoàn tất thanh toán trong 5 phút.",
                data = new
                {
                    maDonDatVe,
                    totalPrice = finalPrice,
                    expiresAt = DateTime.UtcNow.AddMinutes(5)
                }
            });
        }

        [HttpGet("status/{id}")]
        public async Task<IActionResult> CheckBookingStatus(string id)
        {
            var order = await _context.Dondatves
                .Include(d => d.Vexemphims)
                    .ThenInclude(v => v.MalichchieuNavigation)
                        .ThenInclude(l => l.MaphimNavigation)
                .Include(d => d.Vexemphims)
                    .ThenInclude(v => v.MalichchieuNavigation)
                        .ThenInclude(l => l.MaphongNavigation)
                            .ThenInclude(p => p.MarapphimNavigation)
                .FirstOrDefaultAsync(d => d.Madondatve == id);

            if (order == null)
                return NotFound(new { status = "error", message = "Không tìm thấy đơn hàng" });

            var firstTicket = order.Vexemphims.FirstOrDefault();
            var movieTitle = firstTicket?.MalichchieuNavigation?.MaphimNavigation?.Tenphim ?? "Không xác định";
            var showtimeStr = firstTicket?.MalichchieuNavigation != null 
                ? $"{firstTicket.MalichchieuNavigation.Ngaychieu:dd/MM} - {firstTicket.MalichchieuNavigation.Giochieu:HH:mm}"
                : "Không xác định";
            var roomName = firstTicket?.MalichchieuNavigation?.MaphongNavigation?.Tenphong ?? "";
            var theaterName = firstTicket?.MalichchieuNavigation?.MaphongNavigation?.MarapphimNavigation?.Tenrapphim ?? "Ticket Cinema";

            return Ok(new { 
                status = "success", 
                data = new { 
                    order.Madondatve, 
                    order.Trangthai, 
                    order.Tongtien, 
                    movieTitle, 
                    showtime = showtimeStr,
                    theater = theaterName,
                    room = roomName
                } 
            });
        }

        // GET /api/bookings/ticket-by-qr/{qrCode}
        [HttpGet("ticket-by-qr/{qrCode}")]
        public async Task<IActionResult> GetTicketByQRCode(string qrCode)
        {
            var ticket = await _context.Vexemphims
                .Include(v => v.MadondatveNavigation)
                .FirstOrDefaultAsync(v => v.Qrcode != null && v.Qrcode.Contains(qrCode));

            if (ticket == null)
                return NotFound(new { status = "error", message = "Không tìm thấy vé" });

            return Ok(new { status = "success", data = ticket });
        }

        // POST /api/bookings/{id}/cancel (Yêu cầu đăng nhập)
        [Authorize]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(string id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var order = await _context.Dondatves
                .FirstOrDefaultAsync(d => d.Madondatve == id && d.IdKhach == userId);

            if (order == null)
                return NotFound(new { status = "error", message = "Không tìm thấy đơn hàng" });

            if (order.Trangthai != "pending")
                return BadRequest(new { status = "error", message = "Chỉ có thể hủy đơn hàng đang chờ thanh toán" });

            order.Trangthai = "cancelled";
            var tickets = await _context.Vexemphims.Where(v => v.Madondatve == id).ToListAsync();
            tickets.ForEach(t => t.Trangthai = "cancelled");
            await _context.SaveChangesAsync();

            _pendingOrderService.Remove(id);

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
                await _notificationService.CreateAndSendNotificationAsync(userId, "Đã hủy đơn hàng", $"Đơn hàng {id} của bạn đã được hủy thành công.", id, null);
            }
            catch (Exception ex)
            {
                // Ghi log thay vì ném lỗi để API Cancel không bị crash
                Console.WriteLine($"[Error] Gửi thông báo hủy đơn {id} thất bại: {ex.Message}");
            }

            return Ok(new { status = "success", message = "Hủy đơn hàng thành công" });
        }
    }

    public class BookingRequest
    {
        public string ShowtimeId { get; set; } = string.Empty;
        public List<string> SeatIds { get; set; } = new();
        public string PaymentMethod { get; set; } = "vnpay";
        public string? MaVoucher { get; set; }
        public List<ConcessionRequest>? Concessions { get; set; }
    }

    public class ConcessionRequest
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "item" hoặc "combo"
        public int Quantity { get; set; }
    }
}
