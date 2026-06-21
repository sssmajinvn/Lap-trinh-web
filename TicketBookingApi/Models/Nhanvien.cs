using System;
using System.Collections.Generic;

namespace TicketBookingApi.Models;

public partial class Nhanvien
{
    public int IdNhanvien { get; set; }

    public string Manhanvien { get; set; } = null!;

    public string Hoten { get; set; } = null!;

    public DateTime Ngaysinh { get; set; }

    public short? Gioitinh { get; set; }

    public string Sdt { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Matkhau { get; set; } = null!;

    public string? Anhdaidien { get; set; }

    public string Vaitro { get; set; } = null!;

    public DateTime Ngaytao { get; set; }

    public DateTime Ngaycapnhat { get; set; }

    public virtual ICollection<Binhluan> Binhluans { get; set; } = new List<Binhluan>();

    public virtual ICollection<Rapphim> Marapphims { get; set; } = new List<Rapphim>();
}
