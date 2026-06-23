using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using TicketBookingApi.Services;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class CustomerAdminController : ControllerBase
    {
        private static readonly HashSet<string> ValidPaymentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "success", "failed", "pending", "cancelled"
        };

        private readonly ApplicationDbContext _context;
        private readonly IPaymentStatusService _paymentStatusService;

        public CustomerAdminController(
            ApplicationDbContext context,
            IPaymentStatusService paymentStatusService)
        {
            _context = context;
            _paymentStatusService = paymentStatusService;
        }

        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings(
            [FromQuery] string? movieId,
            [FromQuery] string? theaterId,
            [FromQuery] string? status)
        {
            try
            {
                var query = _context.Vexemphims
                    .Include(v => v.MadondatveNavigation)
                        .ThenInclude(d => d.IdKhachNavigation)
                    .Include(v => v.MalichchieuNavigation)
                        .ThenInclude(l => l.MaphimNavigation)
                    .Include(v => v.MalichchieuNavigation)
                        .ThenInclude(l => l.MaphongNavigation)
                            .ThenInclude(p => p.MarapphimNavigation)
                    .Include(v => v.MagheNavigation)
                    .Include(v => v.MadondatveNavigation)
                        .ThenInclude(d => d.Thongtinthanhtoans)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(movieId))
                {
                    query = query.Where(v => v.MalichchieuNavigation.Maphim == movieId);
                }

                if (!string.IsNullOrWhiteSpace(theaterId))
                {
                    query = query.Where(v => v.MalichchieuNavigation.MaphongNavigation.Marapphim == theaterId);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(v => v.MadondatveNavigation.Trangthai == status);
                }

                var bookings = await query
                    .OrderByDescending(v => v.MadondatveNavigation.Ngaydatve)
                    .Select(v => new
                    {
                        ma_ve = v.Mavexemphim,
                        v.Madondatve,
                        ngaydatve = v.MadondatveNavigation.Ngaydatve,
                        tongtien = v.MadondatveNavigation.Tongtien,
                        trangthai_don = v.MadondatveNavigation.Trangthai,
                        id_khach = v.MadondatveNavigation.IdKhachNavigation.IdKhach,
                        hoten = v.MadondatveNavigation.IdKhachNavigation.Hoten,
                        email = v.MadondatveNavigation.IdKhachNavigation.Email,
                        sdt = v.MadondatveNavigation.IdKhachNavigation.Sdt,
                        maphim = v.MalichchieuNavigation.MaphimNavigation.Maphim,
                        tenphim = v.MalichchieuNavigation.MaphimNavigation.Tenphim,
                        v.Maghe,
                        loaighe = v.MagheNavigation.Loaighe,
                        mahangghe = v.MagheNavigation.Mahangghe,
                        soghe = v.MagheNavigation.Soghe,
                        v.Malichchieu,
                        ngaychieu = v.MalichchieuNavigation.Ngaychieu,
                        giochieu = v.MalichchieuNavigation.Giochieu,
                        gioketthuc = v.MalichchieuNavigation.Gioketthuc,
                        gia_ve_lichchieu = v.MalichchieuNavigation.Giave,
                        gia_ve = v.Giave,
                        trangthai_ve = v.Trangthai,
                        maphong = v.MalichchieuNavigation.MaphongNavigation.Maphong,
                        tenphong = v.MalichchieuNavigation.MaphongNavigation.Tenphong,
                        marapphim = v.MalichchieuNavigation.MaphongNavigation.MarapphimNavigation.Marapphim,
                        tenrapphim = v.MalichchieuNavigation.MaphongNavigation.MarapphimNavigation.Tenrapphim,
                        mathanhtoan = v.MadondatveNavigation.Thongtinthanhtoans
                            .OrderByDescending(t => t.Thoidiemthanhtoan)
                            .Select(t => t.Mathanhtoan)
                            .FirstOrDefault(),
                        phuongthucthanhtoan = v.MadondatveNavigation.Thongtinthanhtoans
                            .OrderByDescending(t => t.Thoidiemthanhtoan)
                            .Select(t => t.Phuongthucthanhtoan)
                            .FirstOrDefault(),
                        trangthai_thanhtoan = v.MadondatveNavigation.Thongtinthanhtoans
                            .OrderByDescending(t => t.Thoidiemthanhtoan)
                            .Select(t => t.Trangthai)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(new { status = "success", total = bookings.Count, data = bookings });
            }
            catch (Exception)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi truy xuất danh sách đơn đặt vé" });
            }
        }

        [HttpPut("payments/{id}/status")]
        public async Task<IActionResult> UpdatePaymentStatus(string id, [FromBody] UpdatePaymentStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Trangthai))
            {
                return BadRequest(new { status = "error", message = "Vui lòng cung cấp trạng thái mới" });
            }

            if (!ValidPaymentStatuses.Contains(dto.Trangthai))
            {
                return BadRequest(new
                {
                    status = "error",
                    message = $"Trạng thái không hợp lệ. Giá trị cho phép: {string.Join(", ", ValidPaymentStatuses)}"
                });
            }

            var payment = await _context.Thongtinthanhtoans
                .FirstOrDefaultAsync(t => t.Mathanhtoan == id);

            if (payment == null)
            {
                return NotFound(new { status = "error", message = "Không tìm thấy bản ghi thanh toán với mã: " + id });
            }
            PaymentStatusResult result;
            try
            {
                result = await _paymentStatusService.UpdatePaymentStatusAsync(id, dto.Trangthai);
            }
            catch (Exception)
            {
                return StatusCode(500, new { status = "error", message = "Lỗi khi cập nhật trạng thái thanh toán" });
            }

            return Ok(new
            {
                status = "success",
                message = $"Cập nhật trạng thái thanh toán {id} thành '{result.RequestedStatus}' thành công",
                data = new
                {
                    payment.Mathanhtoan,
                    payment.Madondatve,
                    trangthai = result.PaymentStatus,
                    trangthai_don = result.OrderStatus
                }
            });
        }
    }

    public class UpdatePaymentStatusDto
    {
        public string Trangthai { get; set; } = string.Empty;
    }
}
