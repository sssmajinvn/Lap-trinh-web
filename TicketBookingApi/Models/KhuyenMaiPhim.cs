using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class KhuyenMaiPhim
{
    public int Id { get; set; }

    public int IdKhuyenMai { get; set; }

    public string Maphim { get; set; } = null!;

    public virtual KhuyenMai IdKhuyenMaiNavigation { get; set; } = null!;

    public virtual Phim MaphimNavigation { get; set; } = null!;
}
