using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Thongtinthanhtoan
{
    public string Mathanhtoan { get; set; } = null!;

    public string Phuongthucthanhtoan { get; set; } = null!;

    public string? Paymentgatewaytransactionid { get; set; }

    public int Sotienthanhtoan { get; set; }

    public DateTime Thoidiemthanhtoan { get; set; }

    public string Trangthai { get; set; } = null!;

    public string Madondatve { get; set; } = null!;

    public virtual Dondatve MadondatveNavigation { get; set; } = null!;
}
