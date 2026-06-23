namespace TicketBookingApi.Models.Dtos
{
    public class ShowtimeDto
    {
        public string? Malichchieu { get; set; }
        public DateTime Ngaychieu { get; set; }
        public DateTime Giochieu { get; set; }
        public DateTime Gioketthuc { get; set; }
        public int Giave { get; set; }
        public string Maphim { get; set; } = null!;
        public string Maphong { get; set; } = null!;
    }
}
