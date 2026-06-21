namespace TicketBookingApi.Models.Dtos;

public class RegisterDto
{
    public string Mataikhoan { get; set; } = string.Empty;
    public string Matkhau { get; set; } = string.Empty;
    public string Hoten { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Sdt { get; set; } = string.Empty;
}
