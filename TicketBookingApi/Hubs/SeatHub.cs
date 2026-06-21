using Microsoft.AspNetCore.SignalR;
using TicketBookingApi.Services;

namespace TicketBookingApi.Hubs
{
    public class SeatHub : Hub
    {
        private readonly TemporarySeatLockService _lockService;

        public SeatHub(TemporarySeatLockService lockService)
        {
            _lockService = lockService;
        }

        // Khi client mở trang chọn ghế của một suất chiếu, họ sẽ join group tương ứng
        public async Task JoinShowtime(string showtimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, showtimeId);
        }

        public async Task LeaveShowtime(string showtimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, showtimeId);
        }

        // Dummy: Chức năng này thường được gọi từ Controller khi có thay đổi trạng thái ghế
        public async Task BroadcastSeatStatus(string showtimeId, string seatId, string status)
        {
            await Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new { seatId, status });
        }

        public async Task LockSeat(string showtimeId, string seatId)
        {
            if (_lockService.TryLockSeat(showtimeId, seatId, Context.ConnectionId))
            {
                await Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new 
                {
                    seatIds = new[] { seatId },
                    status = "pending"
                });
            }
        }

        public async Task UnlockSeat(string showtimeId, string seatId)
        {
            if (_lockService.UnlockSeat(showtimeId, seatId, Context.ConnectionId))
            {
                await Clients.Group(showtimeId).SendAsync("ReceiveSeatStatus", new 
                {
                    seatIds = new[] { seatId },
                    status = "available"
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var unlockedSeats = _lockService.UnlockAllForConnection(Context.ConnectionId);
            var grouped = unlockedSeats.GroupBy(s => s.ShowtimeId);
            foreach (var group in grouped)
            {
                var seatIds = group.Select(x => x.SeatId).ToList();
                await Clients.Group(group.Key).SendAsync("ReceiveSeatStatus", new 
                {
                    seatIds = seatIds,
                    status = "available"
                });
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
