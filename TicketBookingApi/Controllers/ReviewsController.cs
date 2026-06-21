using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : 0;
        }

        /// <summary>Kiểm tra user có đã mua vé và xem phim xong chưa</summary>
        private async Task<bool> CanUserReviewAsync(int userId, string movieId)
        {
            return await _context.Dondatves
                .Where(d => d.IdKhach == userId && d.Trangthai == "paid")
                .AnyAsync(d => d.Vexemphims.Any(v =>
                    v.MalichchieuNavigation != null &&
                    v.MalichchieuNavigation.Maphim == movieId &&
                    v.MalichchieuNavigation.Gioketthuc < DateTime.UtcNow));
        }

        // GET /api/reviews/movie/{movieId}
        [HttpGet("movie/{movieId}")]
        public async Task<IActionResult> GetReviews(string movieId)
        {
            var reviews = await _context.Binhluans
                .Where(b => b.Maphim == movieId)
                .OrderByDescending(b => b.Thoidiemdanhgia)
                .Include(b => b.IdKhachNavigation)
                .Select(b => new
                {
                    b.Mabinhluan,
                    b.Maphim,
                    b.Noidung,
                    b.Danhgia,
                    b.Thoidiemdanhgia,
                    b.Noidungreply,
                    hoTen = b.IdKhachNavigation.Hoten,
                    anhDaiDien = b.IdKhachNavigation.Anhdaidien
                })
                .ToListAsync();

            return Ok(new { status = "success", data = reviews });
        }

        // GET /api/reviews/movie/{movieId}/can-review
        [Authorize]
        [HttpGet("movie/{movieId}/can-review")]
        public async Task<IActionResult> CheckCanReview(string movieId)
        {
            var userId = GetUserId();
            var canReview = await CanUserReviewAsync(userId, movieId);

            object? existingReview = null;
            if (canReview)
            {
                existingReview = await _context.Binhluans
                    .Where(b => b.Maphim == movieId && b.IdKhach == userId)
                    .Select(b => new { b.Noidung, b.Danhgia })
                    .FirstOrDefaultAsync();
            }

            return Ok(new { status = "success", canReview, existingReview });
        }

        // POST /api/reviews/movie/{movieId}
        [Authorize]
        [HttpPost("movie/{movieId}")]
        public async Task<IActionResult> PostReview(string movieId, [FromBody] PostReviewDto dto)
        {
            if (dto.DanhGia < 1 || dto.DanhGia > 10)
                return BadRequest(new { status = "error", message = "Điểm đánh giá phải từ 1 đến 10" });

            var userId = GetUserId();

            var canReview = await CanUserReviewAsync(userId, movieId);
            if (!canReview)
                return StatusCode(403, new { status = "error", message = "Bạn cần mua vé và xem phim trước khi đánh giá" });

            var existing = await _context.Binhluans
                .FirstOrDefaultAsync(b => b.Maphim == movieId && b.IdKhach == userId);

            if (existing != null)
            {
                // Cập nhật
                existing.Noidung = dto.NoiDung;
                existing.Danhgia = dto.DanhGia;
                existing.Thoidiemdanhgia = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Cập nhật đánh giá thành công", data = existing });
            }
            else
            {
                // Thêm mới
                var maBinhLuan = "BL" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()[^6..];
                var review = new Binhluan
                {
                    Mabinhluan = maBinhLuan,
                    Maphim = movieId,
                    Noidung = dto.NoiDung,
                    Danhgia = dto.DanhGia,
                    IdKhach = userId,
                    Thoidiemdanhgia = DateTime.UtcNow
                };

                _context.Binhluans.Add(review);
                await _context.SaveChangesAsync();

                return StatusCode(201, new { status = "success", message = "Cảm ơn bạn đã đánh giá phim", data = review });
            }
        }
    }

    public class PostReviewDto
    {
        public string? NoiDung { get; set; }
        public int DanhGia { get; set; }
    }
}
