using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class KhuyenMaiUser
{
    public int Id { get; set; }

    public int IdKhuyenMai { get; set; }

    public int IdKhach { get; set; }

    public virtual Thongtintaikhoan IdKhachNavigation { get; set; } = null!;

    public virtual KhuyenMai IdKhuyenMaiNavigation { get; set; } = null!;
}
