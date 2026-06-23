using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin")]
    [ApiController]
    public class AuthAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthAdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class LoginDto
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { status = "error", message = "Vui lòng nhập email và mật khẩu" });

            var admin = await _context.Nhanviens.FirstOrDefaultAsync(a => a.Email == dto.Email);
            if (admin == null)
                return NotFound(new { status = "error", message = "Tài khoản quản trị không tồn tại" });

            bool passwordMatch = false;
            try
            {
                passwordMatch = BCrypt.Net.BCrypt.Verify(dto.Password, admin.Matkhau);
            }
            catch { }
            if (!passwordMatch && admin.Matkhau != dto.Password)
                return Unauthorized(new { status = "error", message = "Mật khẩu không chính xác" });

            var role = admin.Vaitro == "Quản Lý" ? "QuanLy" : "NhanVien";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.IdNhanvien.ToString()),
                new Claim("admin", "true"),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role),
                new Claim("maNhanVien", admin.Manhanvien)
            };

            var secret = _configuration["Jwt:Secret"] ?? "NHOM7_SECRET_KEY_DEFAULT_FALLBACK_123456789";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new
            {
                status = "success",
                message = "Đăng nhập quản lý thành công",
                token = tokenString,
                staff = new
                {
                    id = admin.IdNhanvien,
                    maNhanVien = admin.Manhanvien,
                    hoTen = admin.Hoten,
                    email = admin.Email,
                    vaiTro = admin.Vaitro
                }
            });
        }
    }
}
