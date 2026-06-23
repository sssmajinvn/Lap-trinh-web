namespace TicketBookingApi.Models.Dtos
{
    public class TmdbSearchResult
    {
        public int Page { get; set; }
        public List<TmdbSearchMovie> Results { get; set; } = new();
        public int TotalPages { get; set; }
        public int TotalResults { get; set; }
    }

    public class TmdbSearchMovie
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public string? ReleaseDate { get; set; }
        public double VoteAverage { get; set; }
    }
}
