using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Combo
{
    public int ComboId { get; set; }

    public string Name { get; set; } = null!;

    public int Price { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsAvailable { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public virtual ICollection<OrderConcession> OrderConcessions { get; set; } = new List<OrderConcession>();
}
