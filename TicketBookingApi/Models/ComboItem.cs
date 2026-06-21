using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class ComboItem
{
    public int ComboItemId { get; set; }

    public int? ComboId { get; set; }

    public int? ItemId { get; set; }

    public int? Quantity { get; set; }

    public virtual Combo? Combo { get; set; }

    public virtual Item? Item { get; set; }
}
