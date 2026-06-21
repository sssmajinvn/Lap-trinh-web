using Microsoft.AspNetCore.Identity;
using TicketBookingApi.Models;

namespace TicketBookingApi.Identity
{
    public class BcryptPasswordHasher : IPasswordHasher<Thongtintaikhoan>
    {
        public string HashPassword(Thongtintaikhoan user, string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public PasswordVerificationResult VerifyHashedPassword(Thongtintaikhoan user, string hashedPassword, string providedPassword)
        {
            try
            {
                if (BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword))
                {
                    return PasswordVerificationResult.Success;
                }
            }
            catch
            {
                // BCrypt.Verify throws if the hash is invalidly formatted
            }
            return PasswordVerificationResult.Failed;
        }
    }
}
