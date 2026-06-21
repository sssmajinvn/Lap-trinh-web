using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class KhuyenMai
{
    public int Id { get; set; }

    public string MaKhuyenMai { get; set; } = null!;

    public string TenKhuyenMai { get; set; } = null!;

    public string? MoTa { get; set; }

    public string LoaiGiam { get; set; } = null!;

    public decimal GiaTriGiam { get; set; }

    public decimal? GiamToiDa { get; set; }

    public decimal? GiaTriDonHangToiThieu { get; set; }

    public int? SoLuongVeToiThieu { get; set; }

    public int? SoLuongMa { get; set; }

    public int? SoLuongDaDung { get; set; }

    public int? SoLanDungToiDaMoiUser { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public string? ApDungCho { get; set; }

    public string? ApDungUser { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<KhuyenMaiPhim> KhuyenMaiPhims { get; set; } = new List<KhuyenMaiPhim>();

    public virtual ICollection<KhuyenMaiUser> KhuyenMaiUsers { get; set; } = new List<KhuyenMaiUser>();

    public virtual ICollection<LichSuKhuyenMai> LichSuKhuyenMais { get; set; } = new List<LichSuKhuyenMai>();
}
