using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/vouchers")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class VoucherAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> ValidLoaiGiam = new(StringComparer.OrdinalIgnoreCase)
        {
            "TIEN", "PHAN_TRAM"
        };

        private static readonly HashSet<string> ValidApDungCho = new(StringComparer.OrdinalIgnoreCase)
        {
            "TAT_CA_PHIM", "PHIM_CU_THE"
        };

        private static readonly HashSet<string> ValidApDungUser = new(StringComparer.OrdinalIgnoreCase)
        {
            "TAT_CA_USER", "BRONZE", "MEMBER", "SILVER", "GOLD", "DIAMOND"
        };

        public VoucherAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? trangThai)
        {
            var query = _context.KhuyenMais
                .Include(v => v.KhuyenMaiPhims)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(v => v.TrangThai == trangThai);

            var vouchers = await query
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new
                {
                    v.Id,
                    v.MaKhuyenMai,
                    v.TenKhuyenMai,
                    v.MoTa,
                    v.LoaiGiam,
                    v.GiaTriGiam,
                    v.GiamToiDa,
                    v.GiaTriDonHangToiThieu,
                    v.SoLuongVeToiThieu,
                    v.SoLuongMa,
                    v.SoLuongDaDung,
                    v.SoLanDungToiDaMoiUser,
                    v.NgayBatDau,
                    v.NgayKetThuc,
                    v.ApDungCho,
                    v.ApDungUser,
                    v.TrangThai,
                    v.CreatedAt,
                    v.UpdatedAt,
                    maphims = v.KhuyenMaiPhims.Select(kp => kp.Maphim).ToList()
                })
                .ToListAsync();

            return Ok(new { status = "success", total = vouchers.Count, data = vouchers });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var voucher = await _context.KhuyenMais
                .Include(v => v.KhuyenMaiPhims)
                    .ThenInclude(kp => kp.MaphimNavigation)
                .Include(v => v.LichSuKhuyenMais)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound(new { status = "error", message = "Không tìm thấy voucher" });

            return Ok(new
            {
                status = "success",
                data = new
                {
                    voucher.Id,
                    voucher.MaKhuyenMai,
                    voucher.TenKhuyenMai,
                    voucher.MoTa,
                    voucher.LoaiGiam,
                    voucher.GiaTriGiam,
                    voucher.GiamToiDa,
                    voucher.GiaTriDonHangToiThieu,
                    voucher.SoLuongVeToiThieu,
                    voucher.SoLuongMa,
                    voucher.SoLuongDaDung,
                    voucher.SoLanDungToiDaMoiUser,
                    voucher.NgayBatDau,
                    voucher.NgayKetThuc,
                    voucher.ApDungCho,
                    voucher.ApDungUser,
                    voucher.TrangThai,
                    phims = voucher.KhuyenMaiPhims.Select(kp => new
                    {
                        kp.Maphim,
                        tenphim = kp.MaphimNavigation.Tenphim
                    }),
                    so_lan_da_dung = voucher.LichSuKhuyenMais.Count
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVoucherDto dto)
        {
            var validation = ValidateVoucherInput(dto.MaKhuyenMai, dto.LoaiGiam, dto.GiaTriGiam,
                dto.NgayBatDau, dto.NgayKetThuc, dto.ApDungCho, dto.ApDungUser, dto.Maphims);
            if (validation != null) return validation;

            var maUpper = dto.MaKhuyenMai.Trim().ToUpperInvariant();
            if (await _context.KhuyenMais.AnyAsync(v => v.MaKhuyenMai == maUpper))
                return BadRequest(new { status = "error", message = "Mã khuyến mãi đã tồn tại" });

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var voucher = new KhuyenMai
                {
                    MaKhuyenMai = maUpper,
                    TenKhuyenMai = dto.TenKhuyenMai,
                    MoTa = dto.MoTa,
                    LoaiGiam = dto.LoaiGiam.ToUpper(),
                    GiaTriGiam = dto.GiaTriGiam,
                    GiamToiDa = dto.GiamToiDa,
                    GiaTriDonHangToiThieu = dto.GiaTriDonHangToiThieu ?? 0,
                    SoLuongVeToiThieu = dto.SoLuongVeToiThieu ?? 1,
                    SoLuongMa = dto.SoLuongMa,
                    SoLuongDaDung = 0,
                    SoLanDungToiDaMoiUser = dto.SoLanDungToiDaMoiUser ?? 1,
                    NgayBatDau = dto.NgayBatDau,
                    NgayKetThuc = dto.NgayKetThuc,
                    ApDungCho = dto.ApDungCho ?? "TAT_CA_PHIM",
                    ApDungUser = dto.ApDungUser ?? "TAT_CA_USER",
                    TrangThai = dto.TrangThai ?? "ACTIVE",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.KhuyenMais.Add(voucher);
                await _context.SaveChangesAsync();

                if (voucher.ApDungCho == "PHIM_CU_THE" && dto.Maphims != null)
                {
                    foreach (var maphim in dto.Maphims.Distinct())
                    {
                        _context.KhuyenMaiPhims.Add(new KhuyenMaiPhim
                        {
                            IdKhuyenMai = voucher.Id,
                            Maphim = maphim
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return StatusCode(201, new { status = "success", message = "Tạo voucher thành công", data = voucher });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                Console.Error.WriteLine($"Create Voucher Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi khi tạo voucher" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVoucherDto dto)
        {
            var voucher = await _context.KhuyenMais
                .Include(v => v.KhuyenMaiPhims)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound(new { status = "error", message = "Không tìm thấy voucher" });

            var loaiGiam = dto.LoaiGiam ?? voucher.LoaiGiam;
            var giaTriGiam = dto.GiaTriGiam ?? voucher.GiaTriGiam;
            var ngayBatDau = dto.NgayBatDau ?? voucher.NgayBatDau;
            var ngayKetThuc = dto.NgayKetThuc ?? voucher.NgayKetThuc;
            var apDungCho = dto.ApDungCho ?? voucher.ApDungCho ?? "TAT_CA_PHIM";
            var apDungUser = dto.ApDungUser ?? voucher.ApDungUser ?? "TAT_CA_USER";

            var validation = ValidateVoucherInput(voucher.MaKhuyenMai, loaiGiam, giaTriGiam,
                ngayBatDau, ngayKetThuc, apDungCho, apDungUser, dto.Maphims);
            if (validation != null) return validation;

            if (!string.IsNullOrWhiteSpace(dto.TenKhuyenMai)) voucher.TenKhuyenMai = dto.TenKhuyenMai;
            if (dto.MoTa != null) voucher.MoTa = dto.MoTa;
            voucher.LoaiGiam = loaiGiam.ToUpper();
            voucher.GiaTriGiam = giaTriGiam;
            if (dto.GiamToiDa.HasValue) voucher.GiamToiDa = dto.GiamToiDa;
            if (dto.GiaTriDonHangToiThieu.HasValue) voucher.GiaTriDonHangToiThieu = dto.GiaTriDonHangToiThieu;
            if (dto.SoLuongVeToiThieu.HasValue) voucher.SoLuongVeToiThieu = dto.SoLuongVeToiThieu;
            if (dto.SoLuongMa.HasValue) voucher.SoLuongMa = dto.SoLuongMa;
            if (dto.SoLanDungToiDaMoiUser.HasValue) voucher.SoLanDungToiDaMoiUser = dto.SoLanDungToiDaMoiUser;
            voucher.NgayBatDau = ngayBatDau;
            voucher.NgayKetThuc = ngayKetThuc;
            voucher.ApDungCho = apDungCho;
            voucher.ApDungUser = apDungUser;
            if (!string.IsNullOrWhiteSpace(dto.TrangThai)) voucher.TrangThai = dto.TrangThai;
            voucher.UpdatedAt = DateTime.UtcNow;

            if (dto.Maphims != null || apDungCho != "PHIM_CU_THE")
            {
                _context.KhuyenMaiPhims.RemoveRange(voucher.KhuyenMaiPhims);
                if (apDungCho == "PHIM_CU_THE" && dto.Maphims != null)
                {
                    foreach (var maphim in dto.Maphims.Distinct())
                    {
                        _context.KhuyenMaiPhims.Add(new KhuyenMaiPhim
                        {
                            IdKhuyenMai = voucher.Id,
                            Maphim = maphim
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Cập nhật voucher thành công", data = voucher });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateVoucherStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TrangThai))
                return BadRequest(new { status = "error", message = "Thiếu trạng thái" });

            var voucher = await _context.KhuyenMais.FindAsync(id);
            if (voucher == null)
                return NotFound(new { status = "error", message = "Không tìm thấy voucher" });

            voucher.TrangThai = dto.TrangThai;
            voucher.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Cập nhật trạng thái thành công", data = voucher });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.KhuyenMais
                .Include(v => v.LichSuKhuyenMais)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound(new { status = "error", message = "Không tìm thấy voucher" });

            if (voucher.LichSuKhuyenMais.Count > 0)
            {
                voucher.TrangThai = "INACTIVE";
                voucher.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Voucher đã có lịch sử sử dụng — đã chuyển sang INACTIVE" });
            }

            var phims = _context.KhuyenMaiPhims.Where(kp => kp.IdKhuyenMai == id);
            _context.KhuyenMaiPhims.RemoveRange(phims);
            _context.KhuyenMais.Remove(voucher);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Xóa voucher thành công" });
        }

        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetUsageHistory(int id)
        {
            var voucher = await _context.KhuyenMais.FindAsync(id);
            if (voucher == null)
                return NotFound(new { status = "error", message = "Không tìm thấy voucher" });

            var history = await _context.LichSuKhuyenMais
                .Where(l => l.IdKhuyenMai == id)
                .Include(l => l.IdKhachNavigation)
                .OrderByDescending(l => l.NgaySuDung)
                .Select(l => new
                {
                    l.Id,
                    l.Madondatve,
                    l.GiaTriGiamThucTe,
                    l.NgaySuDung,
                    khach_hang = new
                    {
                        l.IdKhachNavigation.IdKhach,
                        l.IdKhachNavigation.Hoten,
                        l.IdKhachNavigation.Email
                    }
                })
                .ToListAsync();

            return Ok(new { status = "success", total = history.Count, data = history });
        }

        private IActionResult? ValidateVoucherInput(
            string maKhuyenMai, string loaiGiam, decimal giaTriGiam,
            DateTime ngayBatDau, DateTime ngayKetThuc,
            string? apDungCho, string? apDungUser, List<string>? maphims)
        {
            if (string.IsNullOrWhiteSpace(maKhuyenMai))
                return BadRequest(new { status = "error", message = "Mã khuyến mãi không được để trống" });

            if (!ValidLoaiGiam.Contains(loaiGiam))
                return BadRequest(new { status = "error", message = "loai_giam phải là TIEN hoặc PHAN_TRAM" });

            if (loaiGiam.Equals("PHAN_TRAM", StringComparison.OrdinalIgnoreCase) && (giaTriGiam <= 0 || giaTriGiam > 100))
                return BadRequest(new { status = "error", message = "Giá trị giảm phần trăm phải từ 1 đến 100" });

            if (giaTriGiam <= 0)
                return BadRequest(new { status = "error", message = "gia_tri_giam phải lớn hơn 0" });

            if (ngayKetThuc <= ngayBatDau)
                return BadRequest(new { status = "error", message = "ngay_ket_thuc phải sau ngay_bat_dau" });

            if (!string.IsNullOrWhiteSpace(apDungCho) && !ValidApDungCho.Contains(apDungCho))
                return BadRequest(new { status = "error", message = "ap_dung_cho phải là TAT_CA_PHIM hoặc PHIM_CU_THE" });

            if (!string.IsNullOrWhiteSpace(apDungUser) && !ValidApDungUser.Contains(apDungUser))
                return BadRequest(new { status = "error", message = "ap_dung_user không hợp lệ" });

            if (apDungCho == "PHIM_CU_THE" && (maphims == null || maphims.Count == 0))
                return BadRequest(new { status = "error", message = "Voucher PHIM_CU_THE cần ít nhất 1 maphim" });

            return null;
        }
    }
}
