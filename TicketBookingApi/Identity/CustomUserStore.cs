using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Identity
{
    public class CustomUserStore : IUserStore<Thongtintaikhoan>, IUserPasswordStore<Thongtintaikhoan>, IUserEmailStore<Thongtintaikhoan>
    {
        private readonly ApplicationDbContext _context;

        public CustomUserStore(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IdentityResult> CreateAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            _context.Thongtintaikhoans.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            _context.Thongtintaikhoans.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<Thongtintaikhoan?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (int.TryParse(userId, out int id))
            {
                return await _context.Thongtintaikhoans.FindAsync(new object[] { id }, cancellationToken);
            }
            return null;
        }

        public async Task<Thongtintaikhoan?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Mataikhoan.ToUpper() == normalizedUserName, cancellationToken);
        }

        public Task<string?> GetNormalizedUserNameAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Mataikhoan?.ToUpper());
        }

        public Task<string> GetUserIdAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.IdKhach.ToString());
        }

        public Task<string?> GetUserNameAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Mataikhoan);
        }

        public Task SetNormalizedUserNameAsync(Thongtintaikhoan user, string? normalizedName, CancellationToken cancellationToken)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(Thongtintaikhoan user, string? userName, CancellationToken cancellationToken)
        {
            user.Mataikhoan = userName!;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            _context.Thongtintaikhoans.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        // IUserPasswordStore
        public Task<string?> GetPasswordHashAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Matkhau);
        }

        public Task<bool> HasPasswordAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.Matkhau));
        }

        public Task SetPasswordHashAsync(Thongtintaikhoan user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.Matkhau = passwordHash!;
            return Task.CompletedTask;
        }

        // IUserEmailStore
        public async Task<Thongtintaikhoan?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Email.ToUpper() == normalizedEmail, cancellationToken);
        }

        public Task<string?> GetEmailAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<string?> GetNormalizedEmailAsync(Thongtintaikhoan user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email?.ToUpper());
        }

        public Task SetEmailAsync(Thongtintaikhoan user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email!;
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(Thongtintaikhoan user, bool confirmed, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(Thongtintaikhoan user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
