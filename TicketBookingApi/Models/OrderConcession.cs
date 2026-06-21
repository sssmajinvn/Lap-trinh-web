using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class OrderConcession
{
    public int IdOrderConcession { get; set; }

    public string Madondatve { get; set; } = null!;

    public int? ItemId { get; set; }

    public int? ComboId { get; set; }

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }

    public int? ThanhTien { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Combo? Combo { get; set; }

    public virtual Item? Item { get; set; }

    public virtual Dondatve MadondatveNavigation { get; set; } = null!;
}
