namespace TicketBookingApi.Models.Dtos;

public class CreateVoucherDto
{
    public string MaKhuyenMai { get; set; } = null!;
    public string TenKhuyenMai { get; set; } = null!;
    public string? MoTa { get; set; }
    public string LoaiGiam { get; set; } = null!;
    public decimal GiaTriGiam { get; set; }
    public decimal? GiamToiDa { get; set; }
    public decimal? GiaTriDonHangToiThieu { get; set; }
    public int? SoLuongVeToiThieu { get; set; }
    public int? SoLuongMa { get; set; }
    public int? SoLanDungToiDaMoiUser { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public string? ApDungCho { get; set; }
    public string? ApDungUser { get; set; }
    public string? TrangThai { get; set; }
    public List<string>? Maphims { get; set; }
}

public class UpdateVoucherDto
{
    public string? TenKhuyenMai { get; set; }
    public string? MoTa { get; set; }
    public string? LoaiGiam { get; set; }
    public decimal? GiaTriGiam { get; set; }
    public decimal? GiamToiDa { get; set; }
    public decimal? GiaTriDonHangToiThieu { get; set; }
    public int? SoLuongVeToiThieu { get; set; }
    public int? SoLuongMa { get; set; }
    public int? SoLanDungToiDaMoiUser { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? ApDungCho { get; set; }
    public string? ApDungUser { get; set; }
    public string? TrangThai { get; set; }
    public List<string>? Maphims { get; set; }
}

public class UpdateVoucherStatusDto
{
    public string TrangThai { get; set; } = null!;
}
