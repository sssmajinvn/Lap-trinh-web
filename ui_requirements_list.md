# Danh sách các chức năng cần phát triển UI

Dựa trên phân tích mã nguồn backend (Controllers, Models và Hubs), dưới đây là danh sách các tính năng cần có giao diện người dùng, được chia theo vai trò:

## 1. Phân hệ Khách hàng (User Web/App)

### Hệ thống Đăng nhập & Tài khoản
- [ ] **Trang Đăng nhập:** Email/Số điện thoại, mật khẩu, quên mật khẩu.
- [ ] **Trang Đăng ký:** Tạo tài khoản mới với các thông tin cá nhân.
- [ ] **Quản lý Hồ sơ (Profile):** Cập nhật thông tin cá nhân, đổi mật khẩu.
- [ ] **Lịch sử đặt vé:** Xem danh sách vé đã đặt và trạng thái thanh toán.
- [ ] **Thông báo:** Hiển thị các thông báo từ hệ thống (Real-time qua NotificationHub).

### Duyệt và Tìm kiếm Phim
- [ ] **Trang chủ:** Slider phim hot, danh sách phim đang chiếu và sắp chiếu.
- [ ] **Chi tiết phim:** Thông tin mô tả, trailer, diễn viên, đạo diễn, hashtags.
- [ ] **Đánh giá & Bình luận:** Đọc và gửi đánh giá cho phim.
- [ ] **Tìm kiếm:** Lọc phim theo thể loại, tên, hoặc hashtag.

### Luồng Đặt vé (Booking Flow)
- [ ] **Chọn Xuất chiếu:** Hiển thị lịch chiếu theo rạp và thời gian.
- [ ] **Chọn Ghế (Real-time):** Giao diện dàn ghế trong phòng chiếu (Sử dụng SignalR - SeatHub).
- [ ] **Chọn Combo/Thức ăn (Concessions):** Danh sách bắp nước và combo.
- [ ] **Áp dụng Mã giảm giá (Voucher):** Nhập và kiểm tra mã voucher.
- [ ] **Thanh toán:** Tích hợp các cổng thanh toán (PayOS, VNPAY...) và hiển thị thông tin thanh toán.
- [ ] **Xác nhận đặt vé:** Hiển thị voucher điện tử/QR code sau khi thành công.

---

## 2. Phân hệ Quản trị (Admin Dashboard)

### Quản lý Hệ thống
- [ ] **Dashboard:** Thống kê doanh thu, số lượng vé bán ra, biểu đồ tăng trưởng.
- [ ] **Quản lý Phim:** Thêm, sửa, xóa phim, quản lý ảnh và trailer.
- [ ] **Quản lý Lịch chiếu:** Sắp xếp suất chiếu, phòng chiếu.
- [ ] **Quản lý Rạp & Phòng:** Cấu hình hệ thống rạp và sơ đồ ghế ngồi.

### Quản lý Kinh doanh
- [ ] **Quản lý Combo/Hàng hóa:** Danh sách bắp nước, giá cả.
- [ ] **Quản lý Voucher/Khuyến mãi:** Tạo các chương trình ưu đãi, mã giảm giá.
- [ ] **Quản lý Người dùng:** Danh sách khách hàng, phân quyền (Admin/User).
- [ ] **Quản lý Đơn hàng:** Tra cứu và xử lý các giao dịch đặt vé.

---

## 3. Các thành phần kỹ thuật đặc biệt (UI/UX)
- [ ] **Thông báo đẩy (Toast notifications):** Cho các cập nhật thời gian thực về ghế và ưu đãi.
- [ ] **Hiệu ứng Transitions:** Chuyển cảnh mượt mà giữa các bước đặt vé.
- [ ] **Responsive Design:** Đảm bảo hoạt động tốt trên cả Mobile và Desktop.
