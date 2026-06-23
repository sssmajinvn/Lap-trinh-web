using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;
using BCrypt.Net;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class UserAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentAdminId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var staff = await _context.Nhanviens
                .OrderByDescending(nv => nv.Ngaytao)
                .Select(nv => new
                {
                    id = nv.Manhanvien,
                    id_nhanvien = nv.IdNhanvien,
                    hoten = nv.Hoten,
                    email = nv.Email,
                    sdt = nv.Sdt,
                    vaitro = nv.Vaitro,
                    ngaytao = nv.Ngaytao,
                    loai = "nhanvien",
                    trangthai = "active"
                })
                .ToListAsync();

            var customers = await _context.Thongtintaikhoans
                .OrderByDescending(kh => kh.Ngaytao)
                .Select(kh => new
                {
                    id = kh.Mataikhoan,
                    id_khach = kh.IdKhach,
                    hoten = kh.Hoten,
                    email = kh.Email,
                    sdt = kh.Sdt,
                    vaitro = "user",
                    ngaytao = kh.Ngaytao,
                    loai = "khach",
                    trangthai = kh.Trangthai
                })
                .ToListAsync();

            var allUsers = new List<dynamic>();
            allUsers.AddRange(staff);
            allUsers.AddRange(customers);

            var sortedUsers = allUsers
                .OrderByDescending(u => {
                    var date = u.ngaytao as DateTime?;
                    return date ?? DateTime.MinValue;
                })
                .ToList();

            return Ok(new { status = "success", total = sortedUsers.Count, data = sortedUsers });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var staff = await _context.Nhanviens.FirstOrDefaultAsync(u => u.Manhanvien == id);
            if (staff != null)
                return Ok(new { status = "success", data = staff });

            var customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Mataikhoan == id);
            if (customer != null)
                return Ok(new { status = "success", data = customer });

            if (int.TryParse(id, out var idKhach))
            {
                customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.IdKhach == idKhach);
                if (customer != null)
                    return Ok(new { status = "success", data = customer });
            }

            return NotFound(new { status = "error", message = "User not found" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AdminUpdateUserDto dto)
        {
            var staff = await _context.Nhanviens.FirstOrDefaultAsync(u => u.Manhanvien == id);
            if (staff != null)
            {
                if (dto.Vaitro == "user")
                {
                    return BadRequest(new { status = "error", message = "Không thể chuyển quyền nhân viên sang User." });
                }

                if (!string.IsNullOrWhiteSpace(dto.Hoten)) staff.Hoten = dto.Hoten;
                if (!string.IsNullOrWhiteSpace(dto.Sdt)) staff.Sdt = dto.Sdt;
                if (!string.IsNullOrWhiteSpace(dto.Vaitro)) staff.Vaitro = dto.Vaitro;
                staff.Ngaycapnhat = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Cập nhật nhân viên thành công", data = staff });
            }

            var customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Mataikhoan == id);
            if (customer != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.Vaitro) && dto.Vaitro != "user")
                {
                    return BadRequest(new { status = "error", message = "Không thể cấp quyền Admin cho tài khoản khách hàng." });
                }

                if (!string.IsNullOrWhiteSpace(dto.Hoten)) customer.Hoten = dto.Hoten;
                if (!string.IsNullOrWhiteSpace(dto.Sdt)) customer.Sdt = dto.Sdt;
                customer.Ngaycapnhat = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Cập nhật khách hàng thành công", data = customer });
            }

            return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] AdminUpdateUserStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Trangthai))
                return BadRequest(new { status = "error", message = "Thiếu trạng thái" });

            var customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Mataikhoan == id);
            if (customer != null)
            {
                customer.Trangthai = dto.Trangthai;
                customer.Ngaycapnhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Cập nhật trạng thái thành công", data = customer });
            }

            var staff = await _context.Nhanviens.FirstOrDefaultAsync(u => u.Manhanvien == id);
            if (staff != null)
            {
                return BadRequest(new { status = "error", message = "Tài khoản nhân viên không hỗ trợ thay đổi trạng thái." });
            }

            return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id)
        {
            return await UpdateStatus(id, new AdminUpdateUserStatusDto { Trangthai = "inactive" });
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] AdminChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MatkhauMoi))
                return BadRequest(new { status = "error", message = "Thiếu mật khẩu mới" });

            var hashed = BCrypt.Net.BCrypt.HashPassword(dto.MatkhauMoi, 10);

            var staff = await _context.Nhanviens.FirstOrDefaultAsync(u => u.Manhanvien == id);
            if (staff != null)
            {
                staff.Matkhau = hashed;
                staff.Ngaycapnhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Đổi mật khẩu nhân viên thành công" });
            }

            var customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Mataikhoan == id);
            if (customer != null)
            {
                customer.Matkhau = hashed;
                customer.Ngaycapnhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Đổi mật khẩu khách hàng thành công" });
            }

            return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(u => u.Mataikhoan == id);
            if (customer != null)
                return await DeleteCustomerById(customer.IdKhach);

            var staff = await _context.Nhanviens.FirstOrDefaultAsync(u => u.Manhanvien == id);
            if (staff != null)
            {
                var currentAdminId = GetCurrentAdminId();
                if (currentAdminId.HasValue && staff.IdNhanvien == currentAdminId.Value)
                {
                    return BadRequest(new { status = "error", message = "Không thể xóa tài khoản đang đăng nhập." });
                }

                _context.Nhanviens.Remove(staff);
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Xóa nhân viên thành công" });
            }

            return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });
        }

        private async Task<IActionResult> DeleteCustomerById(int idKhach)
        {
            var activeOrders = await _context.Dondatves
                .CountAsync(d => d.IdKhach == idKhach && (d.Trangthai == "pending" || d.Trangthai == "paid"));

            if (activeOrders > 0)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Không thể xóa khách hàng vì còn đơn pending/paid."
                });
            }

            var customer = await _context.Thongtintaikhoans.FirstOrDefaultAsync(k => k.IdKhach == idKhach);
            if (customer == null)
                return NotFound(new { status = "error", message = "Không tìm thấy người dùng" });

            _context.Thongtintaikhoans.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Xóa khách hàng thành công" });
        }
    }
}
