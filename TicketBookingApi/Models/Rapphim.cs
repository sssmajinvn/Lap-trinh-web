using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Rapphim
{
    public string Marapphim { get; set; } = null!;

    public string Tenrapphim { get; set; } = null!;

    public string Diachi { get; set; } = null!;

    public virtual ICollection<Phongrapphim> Phongrapphims { get; set; } = new List<Phongrapphim>();

    public virtual ICollection<Nhanvien> IdNhanviens { get; set; } = new List<Nhanvien>();
}
