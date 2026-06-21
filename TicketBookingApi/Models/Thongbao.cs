using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Thongbao
{
    public string Mathongbao { get; set; } = null!;

    public string Noidung { get; set; } = null!;

    public DateTime Thoidiemtb { get; set; }

    public string? Trangthai { get; set; }

    public string Tieude { get; set; } = null!;

    public DateTime Ngaytao { get; set; }

    public DateTime? Thoidiemxem { get; set; }

    public int IdKhach { get; set; }

    public string Madondatve { get; set; } = null!;

    public string Maphim { get; set; } = null!;

    public virtual Thongtintaikhoan IdKhachNavigation { get; set; } = null!;

    public virtual Dondatve MadondatveNavigation { get; set; } = null!;

    public virtual Phim MaphimNavigation { get; set; } = null!;
}
