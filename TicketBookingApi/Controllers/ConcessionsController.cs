using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBookingApi.Models;

namespace TicketBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConcessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConcessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/concessions/items
        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _context.Items
                .Where(i => i.IsAvailable == true)
                .Select(i => new
                {
                    i.ItemId,
                    i.Name,
                    i.ItemType,
                    i.Price,
                    i.ImageUrl,
                    i.StockQuantity,
                    i.Unit
                })
                .ToListAsync();

            return Ok(new { status = "success", data = items });
        }

        // GET /api/concessions/combos
        [HttpGet("combos")]
        public async Task<IActionResult> GetCombos()
        {
            var combos = await _context.Combos
                .Where(c => c.IsAvailable == true)
                .Include(c => c.ComboItems)
                    .ThenInclude(ci => ci.Item)
                .OrderBy(c => c.ComboId)
                .Select(c => new
                {
                    c.ComboId,
                    c.Name,
                    c.Price,
                    c.Description,
                    c.ImageUrl,
                    items = c.ComboItems.Select(ci => new
                    {
                        itemId = ci.Item!.ItemId,
                        name = ci.Item.Name,
                        quantity = ci.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { status = "success", data = combos });
        }

        // POST /api/concessions/order
        [Authorize]
        [HttpPost("order")]
        public async Task<IActionResult> CreateConcessionOrder([FromBody] ConcessionOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MaDonDatVe) || request.Items == null || request.Items.Count == 0)
                return BadRequest(new { status = "error", message = "Vui lòng cung cấp madondatve và danh sách combo" });

            var orderExists = await _context.Dondatves
                .AnyAsync(d => d.Madondatve == request.MaDonDatVe);

            if (!orderExists)
                return NotFound(new { status = "error", message = "Không tìm thấy đơn đặt vé" });

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Xóa concessions cũ của đơn hàng này (nếu có), cho phép cập nhật lại
                var oldItems = _context.OrderConcessions
                    .Where(oc => oc.Madondatve == request.MaDonDatVe);
                _context.OrderConcessions.RemoveRange(oldItems);

                int totalComboPrice = 0;

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0) continue;

                    // Ưu tiên combo, sau đó item lẻ
                    if (item.ComboId.HasValue)
                    {
                        var combo = await _context.Combos
                            .FirstOrDefaultAsync(c => c.ComboId == item.ComboId.Value && c.IsAvailable == true);
                        if (combo == null) continue;

                        var unitPrice = combo.Price;
                        totalComboPrice += unitPrice * item.Quantity;

                        _context.OrderConcessions.Add(new OrderConcession
                        {
                            Madondatve = request.MaDonDatVe,
                            ComboId = combo.ComboId,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            ThanhTien = unitPrice * item.Quantity,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else if (item.ItemId.HasValue)
                    {
                        var singleItem = await _context.Items
                            .FirstOrDefaultAsync(i => i.ItemId == item.ItemId.Value && i.IsAvailable == true);
                        if (singleItem == null) continue;

                        var unitPrice = singleItem.Price;
                        totalComboPrice += unitPrice * item.Quantity;

                        _context.OrderConcessions.Add(new OrderConcession
                        {
                            Madondatve = request.MaDonDatVe,
                            ItemId = singleItem.ItemId,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            ThanhTien = unitPrice * item.Quantity,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Cộng tiền combo vào tổng tiền đơn
                var order = await _context.Dondatves.FindAsync(request.MaDonDatVe);
                if (order != null) order.Tongtien += totalComboPrice;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return StatusCode(201, new
                {
                    status = "success",
                    message = "Đã lưu danh sách combo thành công",
                    data = new { madondatve = request.MaDonDatVe, totalComboPrice }
                });
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { status = "error", message = "Lỗi khi lưu đơn combo" });
            }
        }

        // GET /api/concessions/order/{madondatve}
        [Authorize]
        [HttpGet("order/{madondatve}")]
        public async Task<IActionResult> GetConcessionOrder(string madondatve)
        {
            var result = await _context.OrderConcessions
                .Where(oc => oc.Madondatve == madondatve)
                .Include(oc => oc.Combo)
                .Include(oc => oc.Item)
                .Select(oc => new
                {
                    oc.IdOrderConcession,
                    oc.Quantity,
                    oc.UnitPrice,
                    oc.ThanhTien,
                    combo = oc.Combo != null ? new
                    {
                        oc.Combo.ComboId,
                        oc.Combo.Name,
                        oc.Combo.Description,
                        oc.Combo.ImageUrl
                    } : null,
                    item = oc.Item != null ? new
                    {
                        oc.Item.ItemId,
                        oc.Item.Name,
                        oc.Item.ImageUrl
                    } : null
                })
                .ToListAsync();

            return Ok(new { status = "success", data = result });
        }
    }

    public class ConcessionOrderRequest
    {
        public string MaDonDatVe { get; set; } = string.Empty;
        public List<ConcessionItemRequest> Items { get; set; } = new();
    }

    public class ConcessionItemRequest
    {
        public int? ComboId { get; set; }
        public int? ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
