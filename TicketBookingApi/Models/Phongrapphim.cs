using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Phongrapphim
{
    public string Maphong { get; set; } = null!;

    public string Tenphong { get; set; } = null!;

    public int? Soluongghe { get; set; }

    public string Marapphim { get; set; } = null!;

    public virtual ICollection<Ghengoi> Ghengois { get; set; } = new List<Ghengoi>();

    public virtual ICollection<Lichchieu> Lichchieus { get; set; } = new List<Lichchieu>();

    public virtual Rapphim MarapphimNavigation { get; set; } = null!;
}
