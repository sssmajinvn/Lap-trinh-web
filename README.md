# Dự án Hệ Thống Đặt Vé Xem Phim - Nhóm 7

Tài liệu này tóm tắt các công việc đã thực hiện phần giao diện người dùng (UI), cấu trúc dự án và hướng dẫn cho người tiếp tục phát triển.

## 🚀 Những nội dung đã thực hiện trên UI

### 1. Phân tích & Lập kế hoạch UI
- Đã hoàn thành bộ tài liệu yêu cầu chi tiết tại file: [ui_requirements_list.md](file:///c:/BaiTap/Lap-trinh-web/ui_requirements_list.md).
- Phân tích đầy đủ các phân hệ:
    - **Phân hệ khách hàng:** Luồng đặt vé, đăng nhập, lịch sử, trang chủ.
    - **Phân hệ quản trị (Admin):** Dashboard, quản lý rạp/phòng/suất chiếu, thống kê doanh thu.
    - **Thành phần kỹ thuật:** SignalR (Real-time ghế ngồi), Toast notifications, Responsive design.

### 2. Thiết kế Giao diện Mẫu (Prototype)
- Đã triển khai trang **Chờ Thanh Toán**: [PaymentWaiting.html](file:///c:/BaiTap/Lap-trinh-web/PaymentWaiting.html).
- **Đặc điểm thiết kế:**
    - **Phong cách:** Modern Dark Mode & Glassmorphism (hiệu ứng kính mờ).
    - **Typography:** Sử dụng font 'Outfit' từ Google Fonts mang lại cảm giác hiện đại, cao cấp.
    - **Hiệu ứng:** 
        - Animation khi load trang (slide up).
        - Đồng hồ đếm ngược (timer) giữ chỗ thời gian thực.
        - Hiệu ứng phát sáng (glow/drop-shadow) cho các icon trạng thái.
    - **Responsive:** Đã được tối ưu hiển thị tốt trên cả Desktop và Mobile.

### 3. Công nghệ sử dụng & Tích hợp
- **Frontend:** HTML5, CSS3 (Vanilla), JavaScript ES6.
- **Icon & UI:** Font Awesome 6.4.0.
- **Backend Core:** ASP.NET Core Web API (trong thư mục `TicketBookingApi`).
- **Tích hợp Thanh toán:** Các dịch vụ như `MoMoService.cs` và `VNPayService.cs` đã được thiết kế để cung cấp URL thanh toán cho Frontend, luồng này đã được mô phỏng trong trang `PaymentWaiting.html`.

---

## 📂 Cấu trúc thư mục UI

- `/PaymentWaiting.html`: Trang mẫu cho luồng thanh toán và giữ ghế.
- `/ui_requirements_list.md`: Danh sách checklist các tính năng cần phát triển UI.
- `TicketBookingApi/`: Chứa mã nguồn backend (API, Hubs cho SignalR, Services).

---

## 🛠️ Hướng dẫn phát triển tiếp (Next Steps)

Dựa trên danh sách yêu cầu, người tiếp theo có thể bắt đầu với các nhiệm vụ sau:

1.  **Xây dựng Framework:** Quyết định sử dụng React/Vite hoặc giữ nguyên Vanilla JS/HTML cho toàn bộ project (dựa vào độ phức tạp của SignalR).
2.  **Trang chủ Phim:** Thiết kế danh sách phim đang chiếu và sắp chiếu sử dụng Grid layout.
3.  **Sơ đồ ghế (Seat Map):** Đây là phần quan trọng nhất, cần kết hợp Canvas hoặc SVG để hiển thị dàn ghế thời gian thực qua `SeatHub`.
4.  **Tích hợp API:** Kết nối các form Đăng ký/Đăng nhập với các Endpoint đã có trong `AuthController`.

---

> [!TIP]
> Luôn giữ phong cách thiết kế **Premium** với hiệu ứng chuyển cảnh mượt mà và tập trung vào trải nghiệm người dùng (UX) như trang `PaymentWaiting.html` đã demo.
