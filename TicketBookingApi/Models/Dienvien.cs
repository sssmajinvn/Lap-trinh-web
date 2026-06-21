using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Dienvien
{
    public string Madienvien { get; set; } = null!;

    public string Tendienvien { get; set; } = null!;

    public DateTime? Ngaysinh { get; set; }

    public string? Quoctich { get; set; }

    public string? Urlanhdaidien { get; set; }

    public virtual ICollection<Phim> Maphims { get; set; } = new List<Phim>();
}
