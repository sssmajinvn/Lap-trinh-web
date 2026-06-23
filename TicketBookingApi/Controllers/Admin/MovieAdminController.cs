using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;
using TicketBookingApi.Services;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/movies")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class MovieAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private const string TmdbBaseUrl = "https://api.themoviedb.org/3";
        private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";

        public MovieAdminController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: api/admin/movies/search?query=...
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { status = "error", message = "Cần query để tìm phim" });

            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["Tmdb:ApiKey"];
            var url = $"{TmdbBaseUrl}/search/movie?api_key={apiKey}&query={System.Web.HttpUtility.UrlEncode(query)}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { status = "error", message = "Lỗi gọi TMDB" });

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Ok(new { status = "success", data = result?.Results });
        }

        // POST: api/admin/movies
        // body: { tmdbId: int }
        [HttpPost]
        public async Task<IActionResult> AddMovie([FromBody] AddMovieDto dto)
        {
            if (dto == null || dto.TmdbId <= 0)
                return BadRequest(new { status = "error", message = "TmdbId không hợp lệ" });

            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["Tmdb:ApiKey"];
            var detailUrl = $"{TmdbBaseUrl}/movie/{dto.TmdbId}?api_key={apiKey}&append_to_response=videos,credits";
            var response = await client.GetAsync(detailUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { status = "error", message = "Không lấy được chi tiết phim từ TMDB" });

            var json = await response.Content.ReadAsStringAsync();
            var detail = JsonSerializer.Deserialize<TmdbMovieDetail>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (detail == null)
                return BadRequest(new { status = "error", message = "Dữ liệu phim không hợp lệ" });

            // Map to our Phim entity
            var trailerKey = detail.Videos?.Results?.FirstOrDefault(v => v.Type == "Trailer" && v.Site == "YouTube")?.Key;
            var phim = new Phim
            {
                Maphim = System.Guid.NewGuid().ToString(),
                TmdbId = detail.Id.ToString(),
                Tenphim = detail.Title,
                PosterUrl = string.IsNullOrEmpty(detail.PosterPath) ? null : ImageBaseUrl + detail.PosterPath,
                Mota = detail.Overview,
                TrailerUrl = trailerKey != null ? $"https://www.youtube.com/watch?v={trailerKey}" : null,
                Ngayramat = detail.ReleaseDate,
                Trangthai = "coming_soon"
            };

            // Save movie
            await _context.Phims.AddAsync(phim);
            // Save directors
            var directors = detail.Credits?.Crew?.Where(c => c.Job == "Director");
            foreach (var d in directors ?? System.Linq.Enumerable.Empty<TmdbCrewMember>())
            {
                var daodien = new Daodien
                {
                    Madaodien = System.Guid.NewGuid().ToString(),
                    Tendaodien = d.Name,
                    Ngaysinh = null,
                    Quoctich = null,
                    Urlanhdaidien = null
                };
                // Add if not exist (simple check)
                var exists = await _context.Daodiens.AnyAsync(x => x.Tendaodien == d.Name);
                if (!exists) await _context.Daodiens.AddAsync(daodien);
                // Link
                phim.Madaodiens.Add(daodien);
            }
            // Save actors
            var actors = detail.Credits?.Cast?.Take(10);
            foreach (var a in actors ?? System.Linq.Enumerable.Empty<TmdbCastMember>())
            {
                var dienvien = new Dienvien
                {
                    Madienvien = System.Guid.NewGuid().ToString(),
                    Tendienvien = a.Name,
                    Ngaysinh = null,
                    Quoctich = null,
                    Urlanhdaidien = string.IsNullOrEmpty(a.ProfilePath) ? null : ImageBaseUrl + a.ProfilePath
                };
                var exists = await _context.Dienviens.AnyAsync(x => x.Tendienvien == a.Name);
                if (!exists) await _context.Dienviens.AddAsync(dienvien);
                phim.Madienviens.Add(dienvien);
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Thêm phim thành công", movieId = phim.Maphim });
        }

        // PUT: api/admin/movies/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovie(string id, [FromBody] MovieUpdateDto dto)
        {
            var phim = await _context.Phims.FirstOrDefaultAsync(m => m.Maphim == id);
            if (phim == null) return NotFound(new { status = "error", message = "Không tìm thấy phim" });

            if (!string.IsNullOrWhiteSpace(dto.Tenphim)) phim.Tenphim = dto.Tenphim;
            if (dto.Thoiluong > 0) phim.Thoiluong = dto.Thoiluong;
            if (dto.Gioihantuoi >= 0) phim.Gioihantuoi = dto.Gioihantuoi;
            if (dto.Ngayramat != default) phim.Ngayramat = dto.Ngayramat;
            if (!string.IsNullOrWhiteSpace(dto.Trangthai)) phim.Trangthai = dto.Trangthai;
            if (!string.IsNullOrWhiteSpace(dto.PosterUrl)) phim.PosterUrl = dto.PosterUrl;
            if (!string.IsNullOrWhiteSpace(dto.TrailerUrl)) phim.TrailerUrl = dto.TrailerUrl;
            if (!string.IsNullOrWhiteSpace(dto.Mota)) phim.Mota = dto.Mota;

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Cập nhật phim thành công" });
        }

        // DELETE: api/admin/movies/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(string id)
        {
            var phim = await _context.Phims.Include(p => p.Lichchieus).FirstOrDefaultAsync(m => m.Maphim == id);
            if (phim == null) return NotFound(new { status = "error", message = "Phim không tồn tại" });

            // Nếu phim đã có lịch chiếu, không cho xóa mà chỉ cho ẩn
            if (phim.Lichchieus.Any())
            {
                phim.Trangthai = "hidden";
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Phim đã có lịch chiếu nên đã được chuyển sang trạng thái Ẩn thay vì xóa." });
            }

            _context.Phims.Remove(phim);
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Xóa phim thành công" });
        }
    }

    public class AddMovieDto
    {
        public int TmdbId { get; set; }
    }

    public class MovieUpdateDto
    {
        public string Tenphim { get; set; }
        public int Thoiluong { get; set; }
        public int Gioihantuoi { get; set; }
        public DateTime Ngayramat { get; set; }
        public string Trangthai { get; set; }
        public string PosterUrl { get; set; }
        public string TrailerUrl { get; set; }
        public string Mota { get; set; }
    }
}
