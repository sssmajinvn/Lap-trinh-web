using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Item
{
    public int ItemId { get; set; }

    public string Name { get; set; } = null!;

    public string ItemType { get; set; } = null!;

    public int Price { get; set; }

    public string? ImageUrl { get; set; }

    public int? StockQuantity { get; set; }

    public bool? IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Unit { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public virtual ICollection<OrderConcession> OrderConcessions { get; set; } = new List<OrderConcession>();
}
