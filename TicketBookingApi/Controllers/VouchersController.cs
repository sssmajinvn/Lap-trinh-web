using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Models;
using TicketBookingApi.Services;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VouchersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVoucherService _voucherService;

        // Độ ưu tiên rank (dùng để so sánh)
        private static readonly Dictionary<string, int> RankPriority = new()
        {
            ["BRONZE"] = 0,
            ["MEMBER"] = 0,
            ["SILVER"] = 1,
            ["GOLD"] = 2,
            ["DIAMOND"] = 3
        };

        private static readonly Dictionary<string, string> RankNamesVi = new()
        {
            ["BRONZE"] = "Đồng",
            ["MEMBER"] = "Đồng",
            ["SILVER"] = "Bạc",
            ["GOLD"] = "Vàng",
            ["DIAMOND"] = "Kim Cương"
        };

        private static readonly Dictionary<string, decimal> RankDiscountRates = new()
        {
            ["BRONZE"] = 0.00m,
            ["MEMBER"] = 0.00m,
            ["SILVER"] = 0.05m,
            ["GOLD"] = 0.10m,
            ["DIAMOND"] = 0.18m
        };

        public VouchersController(ApplicationDbContext context, IVoucherService voucherService)
        {
            _context = context;
            _voucherService = voucherService;
        }

        private int GetUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : 0;
        }

        private async Task<(string rank, decimal totalSpent)> GetUserRankInfoAsync(int userId)
        {
            var user = await _context.Thongtintaikhoans
                .Where(u => u.IdKhach == userId)
                .Select(u => new { u.HangThanhVien, u.TongChiTieu })
                .FirstOrDefaultAsync();

            return user == null
                ? ("BRONZE", 0)
                : (user.HangThanhVien ?? "BRONZE", user.TongChiTieu);
        }

        // GET /api/vouchers/available?totalPrice=100000&showtimeId=LC001
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableVouchers(
            [FromQuery] decimal totalPrice = 0,
            [FromQuery] string? showtimeId = null)
        {
            var userId = GetUserId();
            var (userRank, totalSpent) = await GetUserRankInfoAsync(userId);

            // Lấy maphim từ showtimeId nếu được truyền
            string? maphim = null;
            if (!string.IsNullOrWhiteSpace(showtimeId))
            {
                maphim = await _context.Lichchieus
                    .Where(l => l.Malichchieu == showtimeId)
                    .Select(l => l.Maphim)
                    .FirstOrDefaultAsync();
            }

            var now = DateTime.UtcNow;
            var vouchers = await _context.KhuyenMais
                .Where(v => v.TrangThai == "ACTIVE" && v.NgayBatDau <= now && v.NgayKetThuc >= now)
                .Include(v => v.KhuyenMaiPhims)
                .Include(v => v.LichSuKhuyenMais)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            var result = new List<object>();

            foreach (var voucher in vouchers)
            {
                bool isEligible = true;
                string? reason = null;

                // 1. Kiểm tra số lượng còn lại
                if (voucher.SoLuongMa > 0 && voucher.SoLuongDaDung >= voucher.SoLuongMa)
                {
                    isEligible = false;
                    reason = "Mã giảm giá này đã hết lượt sử dụng";
                }

                // 2. Kiểm tra đã dùng chưa
                if (isEligible)
                {
                    var usedCount = voucher.LichSuKhuyenMais.Count(l => l.IdKhach == userId);
                    var maxUsesPerUser = voucher.SoLanDungToiDaMoiUser ?? 1;
                    if (usedCount >= maxUsesPerUser)
                    {
                        isEligible = false;
                        reason = "Bạn đã sử dụng mã giảm giá này rồi";
                    }
                }

                // 3. Kiểm tra điều kiện Rank
                if (isEligible && !string.IsNullOrEmpty(voucher.ApDungUser) && voucher.ApDungUser != "TAT_CA_USER")
                {
                    var reqPriority = RankPriority.GetValueOrDefault(voucher.ApDungUser, 0);
                    var userPriority = RankPriority.GetValueOrDefault(userRank, 0);
                    if (userPriority < reqPriority)
                    {
                        isEligible = false;
                        var reqRankName = RankNamesVi.GetValueOrDefault(voucher.ApDungUser, voucher.ApDungUser);
                        reason = $"Chưa đạt hạng {reqRankName} (Hạng hiện tại: {RankNamesVi.GetValueOrDefault(userRank, userRank)})";
                    }
                }

                // 4. Kiểm tra giá trị đơn hàng tối thiểu
                if (isEligible && totalPrice > 0 && voucher.GiaTriDonHangToiThieu > 0)
                {
                    if (totalPrice < voucher.GiaTriDonHangToiThieu)
                    {
                        isEligible = false;
                        var gap = voucher.GiaTriDonHangToiThieu - totalPrice;
                        reason = $"Bạn cần mua thêm {gap:N0}đ để áp dụng mã này";
                    }
                }

                // 5. Kiểm tra điều kiện phim cụ thể
                if (isEligible && voucher.ApDungCho == "PHIM_CU_THE" && !string.IsNullOrEmpty(maphim))
                {
                    var hasPhim = voucher.KhuyenMaiPhims.Any(kp => kp.Maphim == maphim);
                    if (!hasPhim)
                    {
                        isEligible = false;
                        reason = "Mã này không áp dụng cho bộ phim bạn đã chọn";
                    }
                }

                var entry = new
                {
                    mavoucher = voucher.MaKhuyenMai,
                    ten = voucher.TenKhuyenMai,
                    mota = voucher.MoTa,
                    sotien_giam = voucher.LoaiGiam == "TIEN" ? (double)voucher.GiaTriGiam : 0,
                    phantram_giam = voucher.LoaiGiam == "PHAN_TRAM" ? (double)(voucher.GiaTriGiam / 100) : 0.0,
                    donhang_toithieu = (double)(voucher.GiaTriDonHangToiThieu ?? 0),
                    han_sudung = voucher.NgayKetThuc,
                    dieukien_rank = voucher.ApDungUser,
                    is_eligible = isEligible,
                    reason
                };

                result.Add(entry);
            }

            return Ok(new
            {
                status = "success",
                user_rank = new
                {
                    rank = userRank,
                    ten_rank = RankNamesVi.GetValueOrDefault(userRank, userRank),
                    tong_chi_tieu = totalSpent,
                    chiet_khau_tu_dong = RankDiscountRates.GetValueOrDefault(userRank, 0)
                },
                available_vouchers = result
            });
        }

        // POST /api/vouchers/apply
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherDto dto)
        {
            if (dto.TamTinh < 0)
                return BadRequest(new { status = "error", message = "Tạm tính không hợp lệ" });

            var userId = GetUserId();
            var subtotal = dto.TamTinh;

            var (userRank, _) = await GetUserRankInfoAsync(userId);
            var rankDiscountRate = RankDiscountRates.GetValueOrDefault(userRank, 0);

            var discountResult = await _voucherService.CalculateDiscountAsync(userId, subtotal, dto.MaVoucher, dto.ShowtimeId);

            if (!discountResult.IsSuccess)
                return BadRequest(new { status = "error", message = discountResult.ErrorMessage });

            object? appliedVoucher = null;
            if (discountResult.AppliedVoucher != null)
            {
                appliedVoucher = new
                {
                    mavoucher = discountResult.AppliedVoucher.MaKhuyenMai,
                    ten = discountResult.AppliedVoucher.TenKhuyenMai,
                    sotien_duoc_giam = discountResult.VoucherDiscountAmount
                };
            }

            // Xây dựng thông báo ưu đãi
            var thongBaoUuDai = "";
            if (rankDiscountRate > 0)
                thongBaoUuDai += $"Hạng {RankNamesVi.GetValueOrDefault(userRank)} giảm {rankDiscountRate * 100}% (-{discountResult.RankDiscountAmount:N0}đ). ";

            if (appliedVoucher != null)
                thongBaoUuDai += $"Bạn đã tiết kiệm được {discountResult.VoucherDiscountAmount:N0}đ nhờ Voucher!";
            else
                thongBaoUuDai += rankDiscountRate > 0
                    ? "Thêm mã voucher để nhận thêm ưu đãi."
                    : "Hãy thăng hạng hoặc áp dụng voucher để tiết kiệm chi phí.";

            return Ok(new
            {
                status = "success",
                booking_summary = new
                {
                    tam_tinh = subtotal,
                    uu_dai_rank = rankDiscountRate > 0 ? new
                    {
                        rank = userRank,
                        ten_rank = RankNamesVi.GetValueOrDefault(userRank),
                        phantram_giam = rankDiscountRate,
                        sotien_giam_rank = discountResult.RankDiscountAmount
                    } : null,
                    voucher_ap_dung = appliedVoucher,
                    phi_thanh_toan = 0,
                    tong_thanh_toan = discountResult.FinalPrice,
                    thong_bao_uu_dai = thongBaoUuDai.Trim()
                }
            });
        }
    }

    public class ApplyVoucherDto
    {
        public string? MaVoucher { get; set; }
        public decimal TamTinh { get; set; }
        public string? ShowtimeId { get; set; }
    }
}
