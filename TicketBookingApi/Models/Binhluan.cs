using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Binhluan
{
    public string Mabinhluan { get; set; } = null!;

    public string Maphim { get; set; } = null!;

    public string? Noidung { get; set; }

    public int Danhgia { get; set; }

    public DateTime Thoidiemdanhgia { get; set; }

    public int IdKhach { get; set; }

    public int? IdNhanvien { get; set; }

    public string? Noidungreply { get; set; }

    public virtual Thongtintaikhoan IdKhachNavigation { get; set; } = null!;

    public virtual Nhanvien? IdNhanvienNavigation { get; set; }

    public virtual Phim MaphimNavigation { get; set; } = null!;
}
