using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Thongtintaikhoan
{
    public int IdKhach { get; set; }

    public string Mataikhoan { get; set; } = null!;

    public string Hoten { get; set; } = null!;

    public DateTime Ngaysinh { get; set; }

    public short? Gioitinh { get; set; }

    public string Sdt { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Matkhau { get; set; } = null!;

    public string? Anhdaidien { get; set; }

    public DateTime Ngaytao { get; set; }

    public DateTime Ngaycapnhat { get; set; }

    public string Trangthai { get; set; } = null!;

    public string HangThanhVien { get; set; } = null!;

    public decimal TongChiTieu { get; set; }

    public virtual ICollection<Binhluan> Binhluans { get; set; } = new List<Binhluan>();

    public virtual ICollection<Dondatve> Dondatves { get; set; } = new List<Dondatve>();

    public virtual ICollection<KhuyenMaiUser> KhuyenMaiUsers { get; set; } = new List<KhuyenMaiUser>();

    public virtual ICollection<LichSuKhuyenMai> LichSuKhuyenMais { get; set; } = new List<LichSuKhuyenMai>();

    public virtual ICollection<Thongbao> Thongbaos { get; set; } = new List<Thongbao>();
}
