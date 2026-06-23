namespace TicketBookingApi.Models.Dtos;

public class AdminUpdateUserDto
{
    public string? Hoten { get; set; }
    public string? Sdt { get; set; }
    public string? Vaitro { get; set; }
}

public class AdminUpdateUserStatusDto
{
    public string Trangthai { get; set; } = null!;
}

public class AdminChangePasswordDto
{
    public string MatkhauMoi { get; set; } = null!;
}
