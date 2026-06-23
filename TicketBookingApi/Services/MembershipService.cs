using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Services
{
    public interface IMembershipService
    {
        Task ApplyPaymentSpendingAsync(int userId, int amountPaid);
    }

    public class MembershipService : IMembershipService
    {
        private readonly ApplicationDbContext _context;

        public MembershipService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ApplyPaymentSpendingAsync(int userId, int amountPaid)
        {
            var user = await _context.Thongtintaikhoans.FindAsync(userId);
            if (user == null) return;

            user.TongChiTieu += amountPaid;
            user.HangThanhVien = CalculateRank(user.TongChiTieu);
            user.Ngaycapnhat = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public static string CalculateRank(decimal totalSpent)
        {
            if (totalSpent >= 20_000_000) return "DIAMOND";
            if (totalSpent >= 10_000_000) return "GOLD";
            if (totalSpent >= 3_000_000) return "SILVER";
            return "BRONZE";
        }
    }
}
