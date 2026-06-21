using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Lichchieu
{
    public string Malichchieu { get; set; } = null!;

    public DateTime Ngaychieu { get; set; }

    public DateTime Giochieu { get; set; }

    public DateTime Gioketthuc { get; set; }

    public int Giave { get; set; }

    public string Maphim { get; set; } = null!;

    public string Maphong { get; set; } = null!;

    public virtual Phim MaphimNavigation { get; set; } = null!;

    public virtual Phongrapphim MaphongNavigation { get; set; } = null!;

    public virtual ICollection<Vexemphim> Vexemphims { get; set; } = new List<Vexemphim>();
}
