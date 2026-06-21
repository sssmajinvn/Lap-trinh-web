using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Theloai
{
    public string Matheloai { get; set; } = null!;

    public string Tentheloai { get; set; } = null!;

    public string? Mota { get; set; }

    public virtual ICollection<Phim> Maphims { get; set; } = new List<Phim>();
}
