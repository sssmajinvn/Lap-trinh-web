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
    }

    public class AddMovieDto
    {
        public int TmdbId { get; set; }
    }
}
