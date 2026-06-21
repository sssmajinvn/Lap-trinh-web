using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Thongtintaikhoan> _userManager;
        private readonly IConfiguration _configuration;

        // In-memory OTP store (Key: email, Value: (otp, expiresAt))
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime ExpiresAt)> OtpStore = new();

        public AuthController(UserManager<Thongtintaikhoan> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        private string GenerateJwtToken(Thongtintaikhoan user, int expiryDays = 7)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdKhach.ToString()),
                new Claim(ClaimTypes.Name, user.Mataikhoan),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var secretKey = _configuration["Jwt:Secret"] ?? "NHOM7_SECRET_KEY_DEFAULT_FALLBACK_123456789";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _userManager.FindByNameAsync(dto.Mataikhoan) != null)
                return BadRequest(new { status = "error", message = "Tài khoản đã tồn tại" });

            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { status = "error", message = "Email đã được sử dụng" });

            var user = new Thongtintaikhoan
            {
                Mataikhoan = dto.Mataikhoan,
                Hoten = dto.Hoten,
                Email = dto.Email,
                Sdt = dto.Sdt,
                Ngaysinh = DateTime.UtcNow,
                Ngaytao = DateTime.UtcNow,
                Ngaycapnhat = DateTime.UtcNow,
                Trangthai = "active",
                HangThanhVien = "BRONZE",
                TongChiTieu = 0
            };

            var result = await _userManager.CreateAsync(user, dto.Matkhau);
            if (!result.Succeeded)
                return BadRequest(new { status = "error", errors = result.Errors });

            return StatusCode(201, new { status = "success", message = "Đăng ký thành công" });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UsernameOrEmail) ??
                       await _userManager.FindByEmailAsync(dto.UsernameOrEmail);

            if (user == null)
                return NotFound(new { status = "error", message = "Tài khoản không tồn tại" });

            if (user.Trangthai == "disabled")
                return StatusCode(403, new { status = "error", message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ hỗ trợ." });

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized(new { status = "error", message = "Mật khẩu không chính xác" });

            var token = GenerateJwtToken(user, expiryDays: 7);

            return Ok(new
            {
                status = "success",
                message = "Đăng nhập thành công",
                token,
                user = new
                {
                    id = user.IdKhach,
                    maTaiKhoan = user.Mataikhoan,
                    hoTen = user.Hoten,
                    email = user.Email,
                    sdt = user.Sdt,
                    ngaysinh = user.Ngaysinh,
                    gioitinh = user.Gioitinh,
                    anhdaidien = user.Anhdaidien
                }
            });
        }

        // POST /api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { status = "error", message = "Vui lòng nhập địa chỉ email" });

            var email = dto.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound(new { status = "error", message = "Email này không tồn tại trong hệ thống. Vui lòng kiểm tra lại hoặc đăng ký tài khoản mới." });

            if (user.Trangthai == "disabled")
                return StatusCode(403, new { status = "error", message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ hỗ trợ để được giúp đỡ." });

            var otp = new Random().Next(100000, 999999).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            OtpStore[email] = (otp, expiresAt);

            // TODO: Tích hợp email service thực tế để gửi OTP
            return Ok(new
            {
                status = "success",
                message = "Chúng tôi đã gửi mã xác nhận vào email của bạn.",
                debug_otp = otp, // Chỉ giữ lại để dễ test/debug
                expiresIn = "5 minutes"
            });
        }

        // POST /api/auth/verify-otp
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Otp))
                return BadRequest(new { status = "error", message = "Vui lòng cung cấp đầy đủ email và mã OTP" });

            var email = dto.Email.Trim().ToLower();

            if (!OtpStore.TryGetValue(email, out var stored))
                return BadRequest(new { status = "error", message = "Không tìm thấy mã OTP cho email này" });

            if (stored.Otp != dto.Otp)
                return BadRequest(new { status = "error", message = "Mã xác nhận OTP không chính xác" });

            if (DateTime.UtcNow > stored.ExpiresAt)
            {
                OtpStore.TryRemove(email, out _);
                return BadRequest(new { status = "error", message = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới." });
            }

            OtpStore.TryRemove(email, out _);

            // Tạo reset token có thời hạn 15 phút
            var secretKey = _configuration["Jwt:Secret"] ?? "NHOM7_SECRET_KEY_DEFAULT_FALLBACK_123456789";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var resetTokenObj = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim("email", email),
                    new Claim("purpose", "reset_password")
                },
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );
            var resetToken = new JwtSecurityTokenHandler().WriteToken(resetTokenObj);

            return Ok(new { status = "success", message = "Xác thực OTP thành công!", resetToken });
        }

        // POST /api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { status = "error", message = "Vui lòng cung cấp token và mật khẩu mới" });

            var secretKey = _configuration["Jwt:Secret"] ?? "NHOM7_SECRET_KEY_DEFAULT_FALLBACK_123456789";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            ClaimsPrincipal principal;
            try
            {
                principal = new JwtSecurityTokenHandler().ValidateToken(dto.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch
            {
                return BadRequest(new { status = "error", message = "Token không hợp lệ hoặc đã hết hạn" });
            }

            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "reset_password")
                return BadRequest(new { status = "error", message = "Token không hợp lệ" });

            var email = principal.FindFirstValue("email");
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { status = "error", message = "Token không hợp lệ" });

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { status = "error", message = "Không thể đặt lại mật khẩu", errors = result.Errors });

            return Ok(new { status = "success", message = "Khôi phục và cập nhật mật khẩu thành công!" });
        }
    }

    public class ForgotPasswordDto
    {
        public string? Email { get; set; }
    }

    public class VerifyOtpDto
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
    }

    public class ResetPasswordDto
    {
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
    }
}

