using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Vexemphim
{
    public string Mavexemphim { get; set; } = null!;

    public string? Qrcode { get; set; }

    public DateTime Thoigianphathanh { get; set; }

    public string Trangthai { get; set; } = null!;

    public DateTime Thoigianhethan { get; set; }

    public int Giave { get; set; }

    public string Maghe { get; set; } = null!;

    public string Malichchieu { get; set; } = null!;

    public string Madondatve { get; set; } = null!;

    public virtual Dondatve MadondatveNavigation { get; set; } = null!;

    public virtual Ghengoi MagheNavigation { get; set; } = null!;

    public virtual Lichchieu MalichchieuNavigation { get; set; } = null!;
}
