using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin/stats")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class StatsAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StatsAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("bookings-by-movie")]
        public async Task<IActionResult> BookingsByMovie()
        {
            try
            {
                var sql = @"
                    SELECT p.maphim, p.tenphim, p.poster_url,
                           COUNT(v.mavexemphim)::int as so_ve,
                           COALESCE(SUM(v.giave), 0)::int as tong_tien
                    FROM phim p
                    LEFT JOIN lichchieu lc ON p.maphim = lc.maphim
                    LEFT JOIN vexemphim v ON lc.malichchieu = v.malichchieu
                    LEFT JOIN dondatve d ON v.madondatve = d.madondatve AND d.trangthai = 'paid'
                    GROUP BY p.maphim, p.tenphim, p.poster_url
                    ORDER BY so_ve DESC";

                var rows = await _context.Database.SqlQueryRaw<BookingsByMovieRow>(sql).ToListAsync();
                return Ok(new { status = "success", data = rows });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Stats bookings-by-movie Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi thống kê theo phim" });
            }
        }

        [HttpGet("bookings-by-theater")]
        public async Task<IActionResult> BookingsByTheater()
        {
            try
            {
                var sql = @"
                    SELECT r.marapphim, r.tenrapphim, r.diachi,
                           COUNT(v.mavexemphim)::int as so_ve,
                           COALESCE(SUM(v.giave), 0)::int as tong_tien
                    FROM rapphim r
                    LEFT JOIN phongrapphim pr ON r.marapphim = pr.marapphim
                    LEFT JOIN lichchieu lc ON pr.maphong = lc.maphong
                    LEFT JOIN vexemphim v ON lc.malichchieu = v.malichchieu
                    LEFT JOIN dondatve d ON v.madondatve = d.madondatve AND d.trangthai = 'paid'
                    GROUP BY r.marapphim, r.tenrapphim, r.diachi
                    ORDER BY so_ve DESC";

                var rows = await _context.Database.SqlQueryRaw<BookingsByTheaterRow>(sql).ToListAsync();
                return Ok(new { status = "success", data = rows });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Stats bookings-by-theater Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi thống kê theo rạp" });
            }
        }

        [HttpGet("revenue-by-movie")]
        public async Task<IActionResult> RevenueByMovie()
        {
            try
            {
                var sql = @"
                    SELECT p.maphim, p.tenphim,
                           COUNT(v.mavexemphim)::int as so_ve,
                           COALESCE(SUM(v.giave), 0)::int as doanh_thu
                    FROM phim p
                    LEFT JOIN lichchieu lc ON p.maphim = lc.maphim
                    LEFT JOIN vexemphim v ON lc.malichchieu = v.malichchieu
                    LEFT JOIN dondatve d ON v.madondatve = d.madondatve AND d.trangthai = 'paid'
                    GROUP BY p.maphim, p.tenphim
                    ORDER BY doanh_thu DESC";

                var rows = await _context.Database.SqlQueryRaw<RevenueByMovieRow>(sql).ToListAsync();
                return Ok(new { status = "success", data = rows });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Stats revenue-by-movie Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi thống kê doanh thu" });
            }
        }

        [HttpGet("daily-revenue")]
        public async Task<IActionResult> DailyRevenueStats([FromQuery] string? from, [FromQuery] string? to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Vui lòng cung cấp tham số from và to (VD: from=2024-04-01&to=2024-04-30)"
                });
            }

            try
            {
                var sql = @"
                    SELECT 
                        DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh') as ngay,
                        COALESCE(SUM(CASE WHEN d.trangthai = 'paid' THEN d.tongtien ELSE 0 END), 0)::bigint as tong_doanh_thu,
                        COALESCE(SUM(CASE WHEN d.trangthai = 'cancelled' THEN d.tongtien ELSE 0 END), 0)::bigint as tien_hoan_huy,
                        COALESCE(
                            SUM(
                                CASE WHEN d.trangthai = 'paid' THEN 
                                    COALESCE((
                                        SELECT SUM(oc.quantity * oc.unit_price) 
                                        FROM order_concessions oc 
                                        WHERE oc.madondatve = d.madondatve
                                    ), 0)
                                ELSE 0 END
                            ), 0
                        )::bigint as tien_fb
                    FROM dondatve d
                    WHERE DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh') BETWEEN {0} AND {1}
                    GROUP BY DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh')
                    ORDER BY ngay";

                var rows = await _context.Database
                    .SqlQueryRaw<DailyRevenueRow>(sql, from, to)
                    .ToListAsync();

                var dailyData = rows.Select(row =>
                {
                    var tongDoanhThu = (int)row.tong_doanh_thu;
                    var tienFb = (int)row.tien_fb;
                    var tienHoanHuy = (int)row.tien_hoan_huy;
                    var tienBanVeNet = Math.Max(0, tongDoanhThu - tienFb);
                    var tienBanVeUocTinh = tienBanVeNet + tienHoanHuy;

                    return new
                    {
                        ngay = row.ngay,
                        tien_ban_ve = tienBanVeUocTinh,
                        tien_fb = tienFb,
                        tien_hoan_huy = tienHoanHuy,
                        doanh_thu_thuc_te = tongDoanhThu
                    };
                }).ToList();

                return Ok(new { status = "success", data = dailyData });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Daily Revenue Stats Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi thống kê doanh thu theo ngày" });
            }
        }

        [HttpGet("weekly-revenue")]
        public async Task<IActionResult> WeeklyRevenueStats()
        {
            try
            {
                var sql = @"
                    SELECT
                        DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh') as ngay,
                        EXTRACT(DOW FROM d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh')::int as thu_trong_tuan,
                        COALESCE(SUM(CASE WHEN d.trangthai = 'paid' THEN d.tongtien ELSE 0 END), 0)::bigint as doanh_thu
                    FROM dondatve d
                    WHERE DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh') >= CURRENT_DATE - INTERVAL '6 days'
                      AND DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh') <= CURRENT_DATE
                    GROUP BY DATE(d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh'), EXTRACT(DOW FROM d.ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh')
                    ORDER BY ngay ASC";

                var rows = await _context.Database.SqlQueryRaw<WeeklyRevenueRow>(sql).ToListAsync();

                var dayLabels = new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
                var revenueMap = rows.ToDictionary(
                    r => r.ngay.ToString("yyyy-MM-dd"),
                    r => new { nhan = dayLabels[r.thu_trong_tuan], doanh_thu = (int)r.doanh_thu });

                var sevenDays = new List<object>();
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.UtcNow.Date.AddDays(-i);
                    var dateStr = date.ToString("yyyy-MM-dd");
                    var dow = (int)date.DayOfWeek;
                    sevenDays.Add(new
                    {
                        ngay = dateStr,
                        nhan = dayLabels[dow],
                        doanh_thu = revenueMap.TryGetValue(dateStr, out var val) ? val.doanh_thu : 0
                    });
                }

                return Ok(new { status = "success", data = sevenDays });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Weekly Revenue Stats Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi thống kê doanh thu theo tuần" });
            }
        }

        [HttpGet("/api/admin/reports/monthly")]
        public async Task<IActionResult> MonthlyReport([FromQuery] string? month)
        {
            if (string.IsNullOrWhiteSpace(month) || !System.Text.RegularExpressions.Regex.IsMatch(month, @"^\d{4}-\d{2}$"))
            {
                return BadRequest(new { status = "error", message = "Vui lòng truyền tham số month theo định dạng YYYY-MM (VD: 2024-04)" });
            }

            var parts = month.Split('-');
            var year = parts[0];
            var mon = parts[1];

            try
            {
                var summarySql = @"
                    SELECT 
                        COUNT(DISTINCT d.madondatve)::bigint as tong_don,
                        COUNT(v.mavexemphim)::bigint as tong_ve,
                        COALESCE(SUM(CASE WHEN d.trangthai = 'paid' THEN d.tongtien ELSE 0 END), 0)::bigint as tong_doanh_thu
                    FROM dondatve d
                    LEFT JOIN vexemphim v ON d.madondatve = v.madondatve
                    WHERE EXTRACT(YEAR FROM d.ngaydatve) = {0}
                      AND EXTRACT(MONTH FROM d.ngaydatve) = {1}";

                var summary = await _context.Database
                    .SqlQueryRaw<MonthlySummaryRow>(summarySql, int.Parse(year), int.Parse(mon))
                    .FirstOrDefaultAsync() ?? new MonthlySummaryRow();

                var detailSql = @"
                    SELECT p.tenphim,
                           COUNT(v.mavexemphim)::bigint as so_ve,
                           COALESCE(SUM(v.giave), 0)::bigint as doanh_thu
                    FROM dondatve d
                    JOIN vexemphim v ON d.madondatve = v.madondatve
                    JOIN lichchieu lc ON v.malichchieu = lc.malichchieu
                    JOIN phim p ON lc.maphim = p.maphim
                    WHERE d.trangthai = 'paid'
                      AND EXTRACT(YEAR FROM d.ngaydatve) = {0}
                      AND EXTRACT(MONTH FROM d.ngaydatve) = {1}
                    GROUP BY p.tenphim
                    ORDER BY doanh_thu DESC";

                var details = await _context.Database
                    .SqlQueryRaw<MonthlyDetailRow>(detailSql, int.Parse(year), int.Parse(mon))
                    .ToListAsync();

                var ticketSql = @"
                    SELECT d.madondatve, d.ngaydatve, d.tongtien, d.trangthai,
                           t.hoten, t.email,
                           p.tenphim, lc.ngaychieu, lc.giochieu,
                           v.maghe, v.giave as gia_ve
                    FROM dondatve d
                    JOIN thongtintaikhoan t ON d.id_khach = t.id_khach
                    JOIN vexemphim v ON d.madondatve = v.madondatve
                    JOIN lichchieu lc ON v.malichchieu = lc.malichchieu
                    JOIN phim p ON lc.maphim = p.maphim
                    WHERE d.trangthai = 'paid'
                      AND EXTRACT(YEAR FROM d.ngaydatve) = {0}
                      AND EXTRACT(MONTH FROM d.ngaydatve) = {1}
                    ORDER BY d.ngaydatve DESC";

                var tickets = await _context.Database
                    .SqlQueryRaw<MonthlyTicketRow>(ticketSql, int.Parse(year), int.Parse(mon))
                    .ToListAsync();

                using var workbook = new XLWorkbook();

                var sheetSummary = workbook.Worksheets.Add("Tổng Quan");
                sheetSummary.Range("A1:D1").Merge();
                var titleCell = sheetSummary.Cell("A1");
                titleCell.Value = $"BÁO CÁO DOANH THU THÁNG {mon}/{year}";
                titleCell.Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.White);
                titleCell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2F5496"));
                titleCell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                sheetSummary.Cell("A3").Value = "Tổng số đơn đặt vé:";
                sheetSummary.Cell("B3").Value = (int)summary.tong_don;
                sheetSummary.Cell("A4").Value = "Tổng số vé bán ra:";
                sheetSummary.Cell("B4").Value = (int)summary.tong_ve;
                sheetSummary.Cell("A5").Value = "Tổng doanh thu (VND):";
                sheetSummary.Cell("B5").Value = (int)summary.tong_doanh_thu;
                sheetSummary.Column(1).Width = 25;
                sheetSummary.Column(2).Width = 20;

                var sheetDetail = workbook.Worksheets.Add("Doanh Thu Theo Phim");
                sheetDetail.Cell(1, 1).Value = "Tên Phim";
                sheetDetail.Cell(1, 2).Value = "Số Vé";
                sheetDetail.Cell(1, 3).Value = "Doanh Thu (VND)";
                sheetDetail.Row(1).Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                sheetDetail.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2F5496"));

                int detailRow = 2;
                foreach (var row in details)
                {
                    sheetDetail.Cell(detailRow, 1).Value = row.tenphim;
                    sheetDetail.Cell(detailRow, 2).Value = (int)row.so_ve;
                    sheetDetail.Cell(detailRow, 3).Value = (int)row.doanh_thu;
                    detailRow++;
                }
                var totalVe = details.Sum(r => (int)r.so_ve);
                var totalDt = details.Sum(r => (int)r.doanh_thu);
                sheetDetail.Cell(detailRow, 1).Value = "TỔNG CỘNG";
                sheetDetail.Cell(detailRow, 2).Value = totalVe;
                sheetDetail.Cell(detailRow, 3).Value = totalDt;
                sheetDetail.Row(detailRow).Style.Font.SetBold();

                var sheetTickets = workbook.Worksheets.Add("Chi Tiết Vé");
                var headers = new[] { "STT", "Mã Đơn", "Khách Hàng", "Email", "Phim", "Ngày Chiếu", "Giờ Chiếu", "Ghế", "Giá Vé (VND)", "Tổng Tiền (VND)", "Ngày Đặt" };
                for (int i = 0; i < headers.Length; i++)
                    sheetTickets.Cell(1, i + 1).Value = headers[i];
                sheetTickets.Row(1).Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                sheetTickets.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2F5496"));

                for (int i = 0; i < tickets.Count; i++)
                {
                    var t = tickets[i];
                    var r = i + 2;
                    sheetTickets.Cell(r, 1).Value = i + 1;
                    sheetTickets.Cell(r, 2).Value = t.madondatve;
                    sheetTickets.Cell(r, 3).Value = t.hoten;
                    sheetTickets.Cell(r, 4).Value = t.email;
                    sheetTickets.Cell(r, 5).Value = t.tenphim;
                    sheetTickets.Cell(r, 6).Value = t.ngaychieu.ToString("dd/MM/yyyy");
                    sheetTickets.Cell(r, 7).Value = t.giochieu.ToString("HH:mm:ss");
                    sheetTickets.Cell(r, 8).Value = t.maghe;
                    sheetTickets.Cell(r, 9).Value = t.gia_ve;
                    sheetTickets.Cell(r, 10).Value = t.tongtien;
                    sheetTickets.Cell(r, 11).Value = t.ngaydatve.ToString("dd/MM/yyyy");
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"BaoCao_Thang_{month}.xlsx");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Monthly Report Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi khi xuất báo cáo" });
            }
        }

        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> DashboardSummary()
        {
            try
            {
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                var revenueSql = @"
                    SELECT COALESCE(SUM(tongtien), 0)::int as total 
                    FROM dondatve 
                    WHERE DATE(ngaydatve AT TIME ZONE 'Asia/Ho_Chi_Minh') = CAST({0} AS date) AND trangthai = 'paid'";

                var ticketsSql = @"
                    SELECT COUNT(*)::int as total 
                    FROM vexemphim 
                    WHERE DATE(thoigianphathanh) = CAST({0} AS date) AND trangthai = 'active'";

                var moviesSql = @"
                    SELECT COUNT(*)::int as total 
                    FROM phim WHERE trangthai = 'now_showing'";

                var customersSql = @"SELECT COUNT(*)::int as total FROM thongtintaikhoan";

                var hotMoviesSql = @"
                    SELECT p.maphim, p.tenphim, p.poster_url, COUNT(v.mavexemphim)::int as so_ve
                    FROM phim p
                    JOIN lichchieu lc ON p.maphim = lc.maphim
                    JOIN vexemphim v ON lc.malichchieu = v.malichchieu
                    JOIN dondatve d ON v.madondatve = d.madondatve AND d.trangthai = 'paid'
                    GROUP BY p.maphim, p.tenphim, p.poster_url
                    ORDER BY so_ve DESC
                    LIMIT 5";

                var revenue = await _context.Database.SqlQueryRaw<CountRow>(revenueSql, today).FirstOrDefaultAsync();
                var tickets = await _context.Database.SqlQueryRaw<CountRow>(ticketsSql, today).FirstOrDefaultAsync();
                var movies = await _context.Database.SqlQueryRaw<CountRow>(moviesSql).FirstOrDefaultAsync();
                var customers = await _context.Database.SqlQueryRaw<CountRow>(customersSql).FirstOrDefaultAsync();
                var hotMovies = await _context.Database.SqlQueryRaw<HotMovieRow>(hotMoviesSql).ToListAsync();

                return Ok(new
                {
                    status = "success",
                    data = new
                    {
                        totalRevenueToday = revenue?.total ?? 0,
                        ticketSoldToday = tickets?.total ?? 0,
                        moviesNowShowing = movies?.total ?? 0,
                        totalCustomersToday = customers?.total ?? 0,
                        hotMovies
                    }
                });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Dashboard Summary Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi tải dữ liệu tổng quan" });
            }
        }

        // SQL result DTOs
        public class BookingsByMovieRow
        {
            public string maphim { get; set; } = "";
            public string tenphim { get; set; } = "";
            public string? poster_url { get; set; }
            public int so_ve { get; set; }
            public int tong_tien { get; set; }
        }

        public class BookingsByTheaterRow
        {
            public string marapphim { get; set; } = "";
            public string tenrapphim { get; set; } = "";
            public string? diachi { get; set; }
            public int so_ve { get; set; }
            public int tong_tien { get; set; }
        }

        public class RevenueByMovieRow
        {
            public string maphim { get; set; } = "";
            public string tenphim { get; set; } = "";
            public int so_ve { get; set; }
            public int doanh_thu { get; set; }
        }

        public class DailyRevenueRow
        {
            public DateTime ngay { get; set; }
            public long tong_doanh_thu { get; set; }
            public long tien_hoan_huy { get; set; }
            public long tien_fb { get; set; }
        }

        public class WeeklyRevenueRow
        {
            public DateTime ngay { get; set; }
            public int thu_trong_tuan { get; set; }
            public long doanh_thu { get; set; }
        }

        public class MonthlySummaryRow
        {
            public long tong_don { get; set; }
            public long tong_ve { get; set; }
            public long tong_doanh_thu { get; set; }
        }

        public class MonthlyDetailRow
        {
            public string tenphim { get; set; } = "";
            public long so_ve { get; set; }
            public long doanh_thu { get; set; }
        }

        public class MonthlyTicketRow
        {
            public string madondatve { get; set; } = "";
            public DateTime ngaydatve { get; set; }
            public int tongtien { get; set; }
            public string trangthai { get; set; } = "";
            public string hoten { get; set; } = "";
            public string email { get; set; } = "";
            public string tenphim { get; set; } = "";
            public DateTime ngaychieu { get; set; }
            public DateTime giochieu { get; set; }
            public string maghe { get; set; } = "";
            public int gia_ve { get; set; }
        }

        public class CountRow
        {
            public int total { get; set; }
        }

        public class HotMovieRow
        {
            public string maphim { get; set; } = "";
            public string tenphim { get; set; } = "";
            public string? poster_url { get; set; }
            public int so_ve { get; set; }
        }
    }
}
