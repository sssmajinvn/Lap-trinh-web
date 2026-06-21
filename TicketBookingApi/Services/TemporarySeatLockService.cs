using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TicketBookingApi.Hubs;

namespace TicketBookingApi.Services
{
    public class SeatLockInfo
    {
        public string ShowtimeId { get; set; } = string.Empty;
        public string SeatId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
    }

    public class TemporarySeatLockService : IDisposable
    {
        private readonly ConcurrentDictionary<string, SeatLockInfo> _locks = new();
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public TemporarySeatLockService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // Chạy CleanupExpiredLocks mỗi 30 giây
            _timer = new Timer(CleanupExpiredLocks, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private string GetKey(string showtimeId, string seatId) => $"{showtimeId}_{seatId}";

        public bool TryLockSeat(string showtimeId, string seatId, string connectionId)
        {
            var key = GetKey(showtimeId, seatId);
            var newLock = new SeatLockInfo
            {
                ShowtimeId = showtimeId,
                SeatId = seatId,
                ConnectionId = connectionId,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5) // Khóa 5 phút
            };

            // Nếu đã tồn tại mà chưa hết hạn thì không cho khóa. 
            // Nếu đã hết hạn thì ghi đè.
            bool success = false;
            _locks.AddOrUpdate(key, 
                addValueFactory: _ => { success = true; return newLock; },
                updateValueFactory: (_, existingLock) =>
                {
                    if (existingLock.ExpiryTime < DateTime.UtcNow)
                    {
                        success = true;
                        return newLock;
                    }
                    if (existingLock.ConnectionId == connectionId)
                    {
                        // User đang tự gia hạn khóa của chính mình
                        success = true;
                        return newLock;
                    }
                    success = false;
                    return existingLock;
                }
            );

            return success;
        }

        public bool UnlockSeat(string showtimeId, string seatId, string connectionId)
        {
            var key = GetKey(showtimeId, seatId);
            if (_locks.TryGetValue(key, out var existingLock) && existingLock.ConnectionId == connectionId)
            {
                return _locks.TryRemove(key, out _);
            }
            // Mở rộng: Nếu connectionId rỗng, tức là hệ thống cưỡng chế xóa (khi chốt đơn DB thành công)
            if (string.IsNullOrEmpty(connectionId) && _locks.ContainsKey(key))
            {
                return _locks.TryRemove(key, out _);
            }
            return false;
        }

        public List<SeatLockInfo> UnlockAllForConnection(string connectionId)
        {
            var removedLocks = new List<SeatLockInfo>();
            foreach (var kvp in _locks)
            {
                if (kvp.Value.ConnectionId == connectionId)
                {
                    if (_locks.TryRemove(kvp.Key, out var removed))
                    {
                        removedLocks.Add(removed);
                    }
                }
            }
            return removedLocks;
        }

        public List<string> GetLockedSeatsForShowtime(string showtimeId)
        {
            var now = DateTime.UtcNow;
            return _locks.Values
                .Where(l => l.ShowtimeId == showtimeId && l.ExpiryTime > now)
                .Select(l => l.SeatId)
                .ToList();
        }

        private async void CleanupExpiredLocks(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredLocks = new List<SeatLockInfo>();
            foreach (var kvp in _locks)
            {
                if (kvp.Value.ExpiryTime <= now)
                {
                    if (_locks.TryRemove(kvp.Key, out var removed))
                    {
                        expiredLocks.Add(removed);
                    }
                }
            }

            if (expiredLocks.Any())
            {
                using var scope = _serviceProvider.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<SeatHub>>();

                var grouped = expiredLocks.GroupBy(l => l.ShowtimeId);
                foreach (var group in grouped)
                {
                    var seatIds = group.Select(x => x.SeatId).ToList();
                    await hubContext.Clients.Group(group.Key).SendAsync("ReceiveSeatStatus", new
                    {
                        seatIds = seatIds,
                        status = "available" // Nhả ghế quá hạn 5 phút
                    });
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
