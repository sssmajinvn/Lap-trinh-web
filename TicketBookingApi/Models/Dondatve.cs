using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Dondatve
{
    public string Madondatve { get; set; } = null!;

    public DateTime Ngaydatve { get; set; }

    public int Tongtien { get; set; }

    public string Trangthai { get; set; } = null!;

    public int IdKhach { get; set; }

    public virtual Thongtintaikhoan IdKhachNavigation { get; set; } = null!;

    public virtual ICollection<LichSuKhuyenMai> LichSuKhuyenMais { get; set; } = new List<LichSuKhuyenMai>();

    public virtual ICollection<OrderConcession> OrderConcessions { get; set; } = new List<OrderConcession>();

    public virtual ICollection<Thongbao> Thongbaos { get; set; } = new List<Thongbao>();

    public virtual ICollection<Thongtinthanhtoan> Thongtinthanhtoans { get; set; } = new List<Thongtinthanhtoan>();

    public virtual ICollection<Vexemphim> Vexemphims { get; set; } = new List<Vexemphim>();
}
