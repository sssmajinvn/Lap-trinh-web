using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Hashtag
{
    public string Mahashtag { get; set; } = null!;

    public string Tenhashtag { get; set; } = null!;

    public virtual ICollection<Phim> Maphims { get; set; } = new List<Phim>();
}
