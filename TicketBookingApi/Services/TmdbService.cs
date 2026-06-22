using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TicketBookingApi.Services
{
    public interface ITmdbService
    {
        Task<TmdbMovieDetail?> GetMovieDetailAsync(int tmdbId);
    }

    public class TmdbService : ITmdbService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private const string TmdbBaseUrl = "https://api.themoviedb.org/3";
        private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";

        public TmdbService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["Tmdb:ApiKey"];
        }

        public async Task<TmdbMovieDetail?> GetMovieDetailAsync(int tmdbId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{TmdbBaseUrl}/movie/{tmdbId}?api_key={_apiKey}&append_to_response=videos,credits";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var detail = JsonSerializer.Deserialize<TmdbMovieDetail>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return detail;
        }
    }

    // DTOs matching TMDB response (subset needed for mapping)
    public class TmdbMovieDetail
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public DateTime ReleaseDate { get; set; }
        public TmdbVideos? Videos { get; set; }
        public TmdbCredits? Credits { get; set; }
    }

    public class TmdbVideos
    {
        public List<TmdbVideoResult> Results { get; set; } = new();
    }

    public class TmdbVideoResult
    {
        public string? Type { get; set; }
        public string? Site { get; set; }
        public string? Key { get; set; }
    }

    public class TmdbCredits
    {
        public List<TmdbCrewMember>? Crew { get; set; }
        public List<TmdbCastMember>? Cast { get; set; }
    }

    public class TmdbCrewMember
    {
        public string? Job { get; set; }
        public string? Name { get; set; }
    }

    public class TmdbCastMember
    {
        public string? Name { get; set; }
        public string? ProfilePath { get; set; }
    }
}
