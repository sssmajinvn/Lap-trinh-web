using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Phim
{
    public string Maphim { get; set; } = null!;

    public string Tenphim { get; set; } = null!;

    public DateTime Ngayramat { get; set; }

    public string Mota { get; set; } = null!;

    public int Thoiluong { get; set; }

    public int Gioihantuoi { get; set; }

    public string PosterUrl { get; set; } = null!;

    public string TrailerUrl { get; set; } = null!;

    public string Trangthai { get; set; } = null!;

    public string Duoctaoboi { get; set; } = null!;

    public DateTime Duoctaongay { get; set; }

    public string? TmdbId { get; set; }

    public virtual ICollection<Binhluan> Binhluans { get; set; } = new List<Binhluan>();

    public virtual ICollection<KhuyenMaiPhim> KhuyenMaiPhims { get; set; } = new List<KhuyenMaiPhim>();

    public virtual ICollection<Lichchieu> Lichchieus { get; set; } = new List<Lichchieu>();

    public virtual ICollection<Thongbao> Thongbaos { get; set; } = new List<Thongbao>();

    public virtual ICollection<Daodien> Madaodiens { get; set; } = new List<Daodien>();

    public virtual ICollection<Dienvien> Madienviens { get; set; } = new List<Dienvien>();

    public virtual ICollection<Hashtag> Mahashtags { get; set; } = new List<Hashtag>();

    public virtual ICollection<Theloai> Matheloais { get; set; } = new List<Theloai>();
}
