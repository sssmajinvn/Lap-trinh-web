using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Ghengoi
{
    public string Maghe { get; set; } = null!;

    public string Mahangghe { get; set; } = null!;

    public int Soghe { get; set; }

    public string Loaighe { get; set; } = null!;

    public decimal Hesogiaghe { get; set; }

    public string Maphong { get; set; } = null!;

    public virtual Phongrapphim MaphongNavigation { get; set; } = null!;

    public virtual ICollection<Vexemphim> Vexemphims { get; set; } = new List<Vexemphim>();
}
