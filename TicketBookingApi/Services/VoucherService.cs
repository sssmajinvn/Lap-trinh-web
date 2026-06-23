using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Services
{
    public interface IVoucherService
    {
        Task<DiscountResult> CalculateDiscountAsync(int userId, decimal subtotal, string? maVoucher, string? showtimeId);
    }

    public class DiscountResult
    {
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public decimal RankDiscountAmount { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal FinalPrice { get; set; }
        public KhuyenMai? AppliedVoucher { get; set; }
    }

    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;

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

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
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

        public async Task<DiscountResult> CalculateDiscountAsync(int userId, decimal subtotal, string? maVoucher, string? showtimeId)
        {
            var (userRank, _) = await GetUserRankInfoAsync(userId);
            var rankDiscountRate = RankDiscountRates.GetValueOrDefault(userRank, 0);
            var sotienGiamRank = Math.Round(subtotal * rankDiscountRate);

            KhuyenMai? appliedVoucher = null;
            decimal sotienDuocGiamVoucher = 0;

            if (!string.IsNullOrWhiteSpace(maVoucher))
            {
                var now = DateTime.UtcNow;
                var voucher = await _context.KhuyenMais
                    .Include(v => v.LichSuKhuyenMais)
                    .Include(v => v.KhuyenMaiPhims)
                    .FirstOrDefaultAsync(v =>
                        v.MaKhuyenMai == maVoucher &&
                        v.TrangThai == "ACTIVE" &&
                        v.NgayBatDau <= now &&
                        v.NgayKetThuc >= now);

                if (voucher == null)
                    return new DiscountResult { IsSuccess = false, ErrorMessage = "Mã giảm giá không tồn tại, đã hết hạn hoặc ngưng hoạt động" };

                if (voucher.SoLuongMa > 0 && voucher.SoLuongDaDung >= voucher.SoLuongMa)
                    return new DiscountResult { IsSuccess = false, ErrorMessage = "Mã giảm giá này đã hết lượt sử dụng" };

                var usedCount = voucher.LichSuKhuyenMais.Count(l => l.IdKhach == userId);
                if (usedCount >= 1)
                    return new DiscountResult { IsSuccess = false, ErrorMessage = "Bạn đã sử dụng mã giảm giá này rồi" };

                if (!string.IsNullOrEmpty(voucher.ApDungUser) && voucher.ApDungUser != "TAT_CA_USER")
                {
                    var reqPriority = RankPriority.GetValueOrDefault(voucher.ApDungUser, 0);
                    var userPriority = RankPriority.GetValueOrDefault(userRank, 0);
                    if (userPriority < reqPriority)
                    {
                        var reqRankName = RankNamesVi.GetValueOrDefault(voucher.ApDungUser, voucher.ApDungUser);
                        return new DiscountResult { IsSuccess = false, ErrorMessage = $"Mã giảm giá này yêu cầu cấp bậc tối thiểu là hạng {reqRankName}" };
                    }
                }

                if (voucher.GiaTriDonHangToiThieu > 0 && subtotal < voucher.GiaTriDonHangToiThieu)
                {
                    var gap = voucher.GiaTriDonHangToiThieu - subtotal;
                    return new DiscountResult { IsSuccess = false, ErrorMessage = $"Bạn cần mua thêm {gap:N0}đ để áp dụng mã này" };
                }

                if (voucher.ApDungCho == "PHIM_CU_THE" && !string.IsNullOrWhiteSpace(showtimeId))
                {
                    var maphim = await _context.Lichchieus
                        .Where(l => l.Malichchieu == showtimeId)
                        .Select(l => l.Maphim)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(maphim))
                    {
                        var hasPhim = voucher.KhuyenMaiPhims.Any(kp => kp.Maphim == maphim);
                        if (!hasPhim)
                            return new DiscountResult { IsSuccess = false, ErrorMessage = "Mã giảm giá này không áp dụng cho bộ phim bạn đã chọn" };
                    }
                }

                // Tính tiền giảm từ voucher
                if (voucher.LoaiGiam == "TIEN")
                {
                    sotienDuocGiamVoucher = voucher.GiaTriGiam;
                }
                else if (voucher.LoaiGiam == "PHAN_TRAM")
                {
                    sotienDuocGiamVoucher = Math.Round(subtotal * (voucher.GiaTriGiam / 100));
                    if (voucher.GiamToiDa > 0 && sotienDuocGiamVoucher > voucher.GiamToiDa)
                        sotienDuocGiamVoucher = voucher.GiamToiDa!.Value;
                }

                appliedVoucher = voucher;
            }

            // Đảm bảo tổng giảm không vượt quá tạm tính
            var totalDiscount = sotienGiamRank + sotienDuocGiamVoucher;
            if (totalDiscount > subtotal)
            {
                sotienDuocGiamVoucher = subtotal - sotienGiamRank;
                totalDiscount = subtotal;
            }

            return new DiscountResult
            {
                IsSuccess = true,
                RankDiscountAmount = sotienGiamRank,
                VoucherDiscountAmount = sotienDuocGiamVoucher,
                TotalDiscount = totalDiscount,
                FinalPrice = subtotal - totalDiscount,
                AppliedVoucher = appliedVoucher
            };
        }
    }
}
