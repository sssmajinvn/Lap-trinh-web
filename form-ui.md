# 🎬 HƯỚNG DẪN ĐỒNG BỘ GIAO DIỆN (UI DESIGN SYSTEM) - NHÓM 7

Tài liệu này được biên soạn để tất cả thành viên phát triển dự án **TicketBooking** đồng bộ tuyệt đối về giao diện (UI/UX). Mọi màn hình mới cần tuân thủ bảng màu, font chữ, các lớp CSS (classes) và cấu trúc HTML dưới đây.

---

## 🎨 1. BẢNG MÀU CHỦ ĐẠO (DARK CINEMA MODE)
Hệ thống sử dụng tông màu tối sang trọng kết hợp hiệu ứng kính (Glassmorphism) và ánh đỏ/cam neon của rạp phim hiện đại.

| Tên biến CSS | Mã màu (Hex/RGBA) | Vai trò | Minh họa |
| :--- | :--- | :--- | :--- |
| `--bg-primary` | `#0b0f19` | Màu nền chính toàn trang | Nền tối đen pha xanh dương |
| `--bg-secondary` | `#161f30` | Màu nền của khối/section | Đậm màu đá phiến |
| `--glass-bg` | `rgba(22, 31, 48, 0.65)` | Nền thẻ có hiệu ứng mờ | Kết hợp `backdrop-filter: blur(12px)` |
| `--glass-border` | `rgba(255, 255, 255, 0.07)` | Viền mờ cho hiệu ứng kính | Viền trắng trong suốt siêu mỏng |
| `--color-primary` | `#e50914` | Màu chủ đạo (Brand Color) | Đỏ rạp phim (giống Netflix) |
| `--color-primary-hover` | `#ff2e3b` | Màu đỏ khi di chuột qua | Đỏ tươi phát sáng |
| `--color-accent` | `#ff9f43` | Màu cam điểm xuyết | Dùng cho điểm số sao, tag nổi bật |
| `--text-main` | `#f5f6fa` | Chữ chính | Màu trắng ngà dịu mắt |
| `--text-muted` | `#a0aec0` | Chữ phụ, mô tả | Màu xám nhạt |

---

## 📐 2. BỘ PHÔNG CHỮ & KHOẢNG CÁCH (TYPOGRAPHY & SPACING)
* **Font chữ:** Khuyên dùng font `Outfit` hoặc `Inter` của Google Fonts để đem lại trải nghiệm cao cấp.
* **Quy chuẩn Font-size:**
  * Tiêu đề lớn (Banner): `2.5rem` (`40px`), `font-weight: 700`
  * Tiêu đề trang (H1): `2rem` (`32px`), `font-weight: 700`
  * Tiêu đề khối (H2): `1.5rem` (`24px`), `font-weight: 600`
  * Nội dung (Body): `1rem` (`16px`), `font-weight: 400`
  * Chữ chú thích (Small): `0.875rem` (`14px`), `font-weight: 400`
* **Khoảng cách (Spacing Variables):**
  * `--space-xs`: `0.25rem` (`4px`)
  * `--space-sm`: `0.5rem` (`8px`)
  * `--space-md`: `1rem` (`16px`)
  * `--space-lg`: `1.5rem` (`24px`)
  * `--space-xl`: `2rem` (`32px`)

---

## 🧱 3. CÁC THÀNH PHẦN GIAO DIỆN CHUẨN (COMMON COMPONENTS)

### 3.1. Nút Bấm (Buttons)
Mã HTML mẫu sử dụng các class từ `design-system.css`:

```html
<!-- Nút Chính (Primary Button - Đỏ Neon) -->
<button class="btn btn-primary">
    <i class="fa-solid fa-ticket"></i> Đặt Vé Ngay
</button>

<!-- Nút Phụ (Secondary Button - Glassmorphism) -->
<button class="btn btn-secondary">Xem Chi Tiết</button>

<!-- Nút Viền (Outline Button) -->
<button class="btn btn-outline">Quay Lại</button>
```

### 3.2. Ô Nhập Liệu & Bộ Lọc (Inputs & Filters)
Sử dụng cho thanh tìm kiếm phim, lọc thể loại, form viết bình luận:

```html
<!-- Ô Tìm Kiếm kèm Icon -->
<div class="search-box">
    <i class="fa-solid fa-magnifying-glass search-icon"></i>
    <input type="text" class="form-input" placeholder="Tìm tên phim, đạo diễn...">
</div>

<!-- Thẻ Select Dropdown tối giản -->
<select class="form-select">
    <option value="">Chọn thể loại</option>
    <option value="Hành Động">Hành Động</option>
</select>
```

### 3.3. Thẻ Hiển Thị Phim (Movie Card)
Sử dụng hiển thị danh sách phim ở trang chủ và kết quả tìm kiếm. Có hiệu ứng zoom nhẹ và đổ bóng đỏ mờ khi hover.

```html
<div class="movie-card">
    <div class="movie-poster-wrapper">
        <img src="poster-url.jpg" alt="Tên phim" class="movie-poster">
        <span class="movie-age-badge age-pg18">T18</span>
    </div>
    <div class="movie-info">
        <h3 class="movie-title">Lật Mặt 7: Một Điều Ước</h3>
        <div class="movie-meta">
            <span class="movie-duration"><i class="fa-regular fa-clock"></i> 138 phút</span>
            <span class="movie-rating"><i class="fa-solid fa-star"></i> 9.2</span>
        </div>
        <div class="movie-hashtags">
            <span class="badge-hashtag">#lyhai</span>
            <span class="badge-hashtag">#giadinh</span>
        </div>
        <a href="/MoviesMvc/Details/MAPHIM" class="btn btn-primary btn-block text-center mt-2">Đặt Vé</a>
    </div>
</div>
```

### 3.4. Badges (Nhãn dán độ tuổi & thể loại)
* `age-g`: Phổ biến rộng rãi (Xanh lá)
* `age-pg13`: Dưới 13 tuổi cần người giám hộ (Vàng)
* `age-pg16`: Dưới 16 tuổi không được xem (Cam)
* `age-pg18`: Phim dành cho người trên 18 tuổi (Đỏ)

```html
<span class="badge badge-genre">Hành động</span>
<span class="movie-age-badge age-pg18">T18</span>
```

---

## 🎬 4. QUY CHUẨN LAYOUT CÁC TRANG CỦA TỪNG CHỨC NĂNG

### 4.1. Trang Chủ (Home Page)
* **Phần đầu:** Slider Phim Hot thiết kế tràn viền rộng (Full-width banner) với nút gọi hành động nổi bật (Call To Action).
* **Phần thân:** Grid Layout chia làm 4 cột (Responsive xuống 2 cột trên Mobile, 1 cột trên điện thoại nhỏ).
* Sử dụng CSS Flexbox/Grid để sắp xếp 2 Tab: **Phim Đang Chiếu** và **Phim Sắp Chiếu**.

### 4.2. Trang Chi Tiết Phim & Đánh Giá
* **Ảnh nền mờ (Blurred Backdrop Background):** Sử dụng Poster phim làm mờ 20px đặt làm ảnh nền của phần header để tạo chiều sâu nghệ thuật.
* **Khối Trailer:** Dùng iframe nhúng từ Youtube với tỉ lệ chuẩn `16:9` và bo góc.
* **Form Bình Luận:** Hộp nhập văn bản tối màu (`background: rgba(255,255,255,0.04)`) có khu vực chọn chấm điểm từ 1 đến 5 sao bằng cách nhấp chuột.

### 4.3. Chọn Lịch Chiếu (Booking Flow)
* **Thanh chọn ngày (Date Selector):** Trình bày dạng danh sách trượt ngang. Mỗi phần tử hiển thị Thứ và Ngày (ví dụ: `Thứ 2 / 22`). Khi được chọn, phần tử đó sẽ có nền đỏ `--color-primary` và hiệu ứng đổ bóng.
* **Danh sách suất chiếu:** Gom nhóm theo từng Rạp chiếu (`Marapphim`). Mỗi giờ chiếu là một thẻ nút nhỏ `.btn-time` bo góc. Giờ đã qua thì bị disabled và mờ đi.

---

## 🚀 5. HƯỚNG DẪN NHANH ĐỂ CỘNG SỰ SỬ DỤNG
1. **Liên kết file styles:** Đảm bảo trang web của bạn sử dụng Layout chung `_Layout.cshtml`, nó đã tự động nhúng tệp [design-system.css](file:///d:/LapTrinhWeb%20.NetCore/Do%20an%20.net/Lap-trinh-web/TicketBookingApi/wwwroot/css/design-system.css).
2. **Không dùng màu thủ công:** Tuyệt đối tránh ghi đè màu bằng style nội dòng (inline style) hoặc dùng màu tùy tiện. Sử dụng các biến `--color-primary`, `--bg-secondary`... của hệ thống.
3. **Tham chiếu trực tiếp:** Truy cập địa chỉ `/styleguide` trên trình duyệt lúc chạy server nội bộ để copy-paste chính xác các mã HTML mẫu đã dựng sẵn.
