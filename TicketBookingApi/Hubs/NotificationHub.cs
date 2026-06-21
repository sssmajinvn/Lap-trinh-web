using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TicketBookingApi.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Hub này được sử dụng chủ yếu để Server push dữ liệu về cho Client
        // Nếu Frontend cần gọi lên để báo đã đọc, cũng có thể viết hàm ở đây (tùy chọn)
        
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
