using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Services
{
    public interface IEmailService
    {
        Task SendBookingCancelledEmailAsync(string orderId);
    }

    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendBookingCancelledEmailAsync(string orderId)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var fromEmail = smtpSection["FromEmail"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogInformation("Skip cancel email for {OrderId} because SMTP is not configured.", orderId);
                return;
            }

            var info = await _context.Dondatves
                .Where(d => d.Madondatve == orderId)
                .Select(d => new
                {
                    d.Madondatve,
                    d.Tongtien,
                    customerEmail = d.IdKhachNavigation.Email,
                    customerName = d.IdKhachNavigation.Hoten,
                    firstTicket = d.Vexemphims
                        .OrderBy(v => v.Mavexemphim)
                        .Select(v => new
                        {
                            movieId = v.MalichchieuNavigation.MaphimNavigation.Maphim,
                            movieName = v.MalichchieuNavigation.MaphimNavigation.Tenphim,
                            theaterName = v.MalichchieuNavigation.MaphongNavigation.MarapphimNavigation.Tenrapphim,
                            roomName = v.MalichchieuNavigation.MaphongNavigation.Tenphong,
                            ngayChieu = v.MalichchieuNavigation.Ngaychieu,
                            gioChieu = v.MalichchieuNavigation.Giochieu
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (info == null || string.IsNullOrWhiteSpace(info.customerEmail) || info.firstTicket == null)
            {
                return;
            }

            var port = int.TryParse(smtpSection["Port"], out var parsedPort) ? parsedPort : 587;
            var enableSsl = !bool.TryParse(smtpSection["EnableSsl"], out var parsedSsl) || parsedSsl;
            var subject = $"Thong bao huy don {info.Madondatve}";
            var body = $"""
                Xin chao {info.customerName},

                Don hang {info.Madondatve} cua ban da bi huy.

                Phim: {info.firstTicket.movieName}
                Rap: {info.firstTicket.theaterName}
                Phong: {info.firstTicket.roomName}
                Ngay chieu: {info.firstTicket.ngayChieu:dd/MM/yyyy}
                Gio chieu: {info.firstTicket.gioChieu:HH:mm}
                So tien: {info.Tongtien:N0} VND

                Neu co giao dich hoan tien, he thong se xu ly theo chinh sach hien hanh.
                """;

            using var message = new MailMessage(fromEmail, info.customerEmail, subject, body);
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }
    }
}
