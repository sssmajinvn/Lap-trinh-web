using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class LichSuKhuyenMai
{
    public int Id { get; set; }

    public int IdKhuyenMai { get; set; }

    public int IdKhach { get; set; }

    public string Madondatve { get; set; } = null!;

    public decimal GiaTriGiamThucTe { get; set; }

    public DateTime? NgaySuDung { get; set; }

    public virtual Thongtintaikhoan IdKhachNavigation { get; set; } = null!;

    public virtual KhuyenMai IdKhuyenMaiNavigation { get; set; } = null!;

    public virtual Dondatve MadondatveNavigation { get; set; } = null!;
}
