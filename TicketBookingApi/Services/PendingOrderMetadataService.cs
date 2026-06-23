using Microsoft.Extensions.Caching.Memory;

namespace TicketBookingApi.Services
{
    public class PendingOrderMetadata
    {
        public int UserId { get; set; }
        public int? VoucherId { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public decimal RankDiscountAmount { get; set; }
    }

    public interface IPendingOrderMetadataService
    {
        void Set(string maDonDatVe, PendingOrderMetadata metadata);
        PendingOrderMetadata? Get(string maDonDatVe);
        void Remove(string maDonDatVe);
    }

    public class PendingOrderMetadataService : IPendingOrderMetadataService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

        public PendingOrderMetadataService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private static string Key(string maDonDatVe) => $"pending_order:{maDonDatVe}";

        public void Set(string maDonDatVe, PendingOrderMetadata metadata)
        {
            _cache.Set(Key(maDonDatVe), metadata, Ttl);
        }

        public PendingOrderMetadata? Get(string maDonDatVe)
        {
            return _cache.TryGetValue(Key(maDonDatVe), out PendingOrderMetadata? meta) ? meta : null;
        }

        public void Remove(string maDonDatVe)
        {
            _cache.Remove(Key(maDonDatVe));
        }
    }
}
