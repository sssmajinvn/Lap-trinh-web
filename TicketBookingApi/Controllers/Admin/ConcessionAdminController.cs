using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBookingApi.Models;
using TicketBookingApi.Models.Dtos;

namespace TicketBookingApi.Controllers.Admin
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class ConcessionAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const string DefaultComboImage =
            "https://images.unsplash.com/photo-1572177191856-3cde618dee1f?q=80&w=500&auto=format&fit=crop";

        private static readonly HashSet<string> ValidItemTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "food", "drink", "accessory"
        };

        public ConcessionAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================== ITEMS ========================

        [HttpGet("items")]
        public async Task<IActionResult> GetAllItems()
        {
            try
            {
                var items = await _context.Items
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
                return Ok(new { status = "success", total = items.Count, data = items });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return StatusCode(500, new { status = "error", message = "Lỗi truy xuất items" });
            }
        }

        [HttpPost("items")]
        public async Task<IActionResult> CreateItem([FromBody] CreateItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.ItemType) || dto.Price <= 0)
                return BadRequest(new { status = "error", message = "Vui lòng nhập name, item_type và price" });

            if (!ValidItemTypes.Contains(dto.ItemType))
                return BadRequest(new { status = "error", message = "item_type phải là food, drink hoặc accessory" });

            var item = new Item
            {
                Name = dto.Name,
                ItemType = dto.ItemType.ToLower(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl,
                StockQuantity = dto.StockQuantity ?? 0,
                Unit = dto.Unit,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { status = "success", message = "Thêm sản phẩm thành công", data = item });
        }

        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemDto dto)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound(new { status = "error", message = "Không tìm thấy sản phẩm" });

            if (!string.IsNullOrWhiteSpace(dto.Name)) item.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.ItemType))
            {
                if (!ValidItemTypes.Contains(dto.ItemType))
                    return BadRequest(new { status = "error", message = "item_type phải là food, drink hoặc accessory" });
                item.ItemType = dto.ItemType.ToLower();
            }
            if (dto.Price.HasValue) item.Price = dto.Price.Value;
            if (dto.ImageUrl != null) item.ImageUrl = dto.ImageUrl;
            if (dto.StockQuantity.HasValue) item.StockQuantity = dto.StockQuantity;
            if (dto.IsAvailable.HasValue) item.IsAvailable = dto.IsAvailable;
            if (dto.Unit != null) item.Unit = dto.Unit;

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Cập nhật sản phẩm thành công", data = item });
        }

        [HttpDelete("items/{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound(new { status = "error", message = "Không tìm thấy sản phẩm" });

            try
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Xoá sản phẩm thành công" });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Không thể xoá vì sản phẩm đang được sử dụng trong đơn hàng hoặc combo."
                });
            }
        }

        // ======================== ACCESSORIES ========================

        [HttpGet("accessories")]
        public async Task<IActionResult> GetAllAccessories()
        {
            try
            {
                var items = await _context.Items
                    .Where(i => i.ItemType == "accessory")
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
                return Ok(new { status = "success", total = items.Count, data = items });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return StatusCode(500, new { status = "error", message = "Lỗi truy xuất phụ kiện" });
            }
        }

        [HttpPost("accessories")]
        public async Task<IActionResult> CreateAccessory([FromBody] CreateItemDto dto)
        {
            dto.ItemType = "accessory";
            return await CreateItem(dto);
        }

        [HttpPut("accessories/{id}")]
        public async Task<IActionResult> UpdateAccessory(int id, [FromBody] UpdateItemDto dto)
        {
            dto.ItemType = "accessory";
            return await UpdateItem(id, dto);
        }

        [HttpDelete("accessories/{id}")]
        public async Task<IActionResult> DeleteAccessory(int id)
        {
            return await DeleteItem(id);
        }

        // ======================== COMBOS ========================

        [HttpGet("combos")]
        public async Task<IActionResult> GetAllCombos()
        {
            try
            {
                var combos = await _context.Combos
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
                        c.IsAvailable,
                        items = c.ComboItems.Select(ci => new
                        {
                            combo_item_id = ci.ComboItemId,
                            item_id = ci.Item!.ItemId,
                            name = ci.Item.Name,
                            quantity = ci.Quantity
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new { status = "success", total = combos.Count, data = combos });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return StatusCode(500, new { status = "error", message = "Lỗi truy xuất combos" });
            }
        }

        [HttpPost("combos")]
        public async Task<IActionResult> CreateCombo([FromBody] CreateComboDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
                return BadRequest(new { status = "error", message = "Vui lòng nhập name và price" });

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var finalImageUrl = !string.IsNullOrWhiteSpace(dto.ImageUrl) ? dto.ImageUrl : DefaultComboImage;

                var combo = new Combo
                {
                    Name = dto.Name,
                    Price = dto.Price,
                    Description = dto.Description,
                    ImageUrl = finalImageUrl,
                    IsAvailable = true
                };

                _context.Combos.Add(combo);
                await _context.SaveChangesAsync();

                if (dto.Items != null && dto.Items.Count > 0)
                {
                    foreach (var item in dto.Items)
                    {
                        _context.ComboItems.Add(new ComboItem
                        {
                            ComboId = combo.ComboId,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity > 0 ? item.Quantity : 1
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return StatusCode(201, new { status = "success", message = "Thêm combo thành công", data = combo });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                Console.Error.WriteLine($"Create Combo Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi khi thêm combo" });
            }
        }

        [HttpPut("combos/{id}")]
        public async Task<IActionResult> UpdateCombo(int id, [FromBody] UpdateComboDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var combo = await _context.Combos.FindAsync(id);
                if (combo == null)
                    return NotFound(new { status = "error", message = "Không tìm thấy combo" });

                if (!string.IsNullOrWhiteSpace(dto.Name)) combo.Name = dto.Name;
                if (dto.Price.HasValue) combo.Price = dto.Price.Value;
                if (dto.Description != null) combo.Description = dto.Description;
                if (dto.ImageUrl != null) combo.ImageUrl = dto.ImageUrl;
                if (dto.IsAvailable.HasValue) combo.IsAvailable = dto.IsAvailable;

                if (dto.Items != null && dto.Items.Count > 0)
                {
                    var oldItems = _context.ComboItems.Where(ci => ci.ComboId == id);
                    _context.ComboItems.RemoveRange(oldItems);

                    foreach (var item in dto.Items)
                    {
                        _context.ComboItems.Add(new ComboItem
                        {
                            ComboId = id,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity > 0 ? item.Quantity : 1
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(new { status = "success", message = "Cập nhật combo thành công", data = combo });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                Console.Error.WriteLine($"Update Combo Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi khi cập nhật combo" });
            }
        }

        [HttpDelete("combos/{id}")]
        public async Task<IActionResult> DeleteCombo(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
                return NotFound(new { status = "error", message = "Không tìm thấy combo" });

            try
            {
                var comboItems = _context.ComboItems.Where(ci => ci.ComboId == id);
                _context.ComboItems.RemoveRange(comboItems);
                _context.Combos.Remove(combo);
                await _context.SaveChangesAsync();
                return Ok(new { status = "success", message = "Xoá combo thành công" });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Không thể xoá vì combo đang được sử dụng trong đơn hàng."
                });
            }
        }

        // ======================== POS ========================

        [HttpPost("pos/concessions/book")]
        public async Task<IActionResult> CreatePOSConcessionOrder([FromBody] PosConcessionBookDto dto)
        {
            if (dto.Concessions == null || dto.Concessions.Count == 0)
                return BadRequest(new { status = "error", message = "Vui lòng chọn ít nhất 1 món" });

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int tongtien = dto.Concessions.Sum(c => c.Price * c.Quantity);
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var maDonDatVe = "POS" + ts.Substring(Math.Max(0, ts.Length - 7));

                var guest = await _context.Thongtintaikhoans
                    .Where(t => t.Mataikhoan == "GUEST_POS")
                    .Select(t => t.IdKhach)
                    .FirstOrDefaultAsync();

                var guestId = guest != 0 ? guest : 10;

                _context.Dondatves.Add(new Dondatve
                {
                    Madondatve = maDonDatVe,
                    Tongtien = tongtien,
                    Trangthai = "paid",
                    IdKhach = guestId,
                    Ngaydatve = DateTime.UtcNow
                });

                var maThanhToan = "PAY" + ts.Substring(Math.Max(0, ts.Length - 6));
                _context.Thongtinthanhtoans.Add(new Thongtinthanhtoan
                {
                    Mathanhtoan = maThanhToan,
                    Phuongthucthanhtoan = "cash",
                    Sotienthanhtoan = tongtien,
                    Trangthai = "success",
                    Madondatve = maDonDatVe,
                    Thoidiemthanhtoan = DateTime.UtcNow
                });

                foreach (var c in dto.Concessions)
                {
                    _context.OrderConcessions.Add(new OrderConcession
                    {
                        Madondatve = maDonDatVe,
                        ComboId = c.ComboId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Price,
                        ThanhTien = c.Price * c.Quantity,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return StatusCode(201, new
                {
                    status = "success",
                    message = "Thanh toán hoá đơn tại quầy thành công",
                    data = new { maDonDatVe, tongtien }
                });
            }
            catch (Exception e)
            {
                await tx.RollbackAsync();
                Console.Error.WriteLine($"Create POS Order Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi khi tạo hoá đơn tại quầy" });
            }
        }

        [HttpGet("pos/concessions/orders")]
        public async Task<IActionResult> GetPOSConcessionOrders()
        {
            try
            {
                var orders = await _context.Dondatves
                    .Where(d => d.Madondatve.StartsWith("POS"))
                    .Include(d => d.OrderConcessions)
                        .ThenInclude(oc => oc.Combo)
                    .OrderByDescending(d => d.Ngaydatve)
                    .Select(d => new
                    {
                        d.Madondatve,
                        d.Ngaydatve,
                        d.Tongtien,
                        items = d.OrderConcessions
                            .Where(oc => oc.ComboId != null)
                            .Select(oc => new
                            {
                                combo_id = oc.ComboId,
                                name = oc.Combo!.Name,
                                quantity = oc.Quantity,
                                unit_price = oc.UnitPrice
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(new { status = "success", total = orders.Count, data = orders });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Get POS Orders Error: {e}");
                return StatusCode(500, new { status = "error", message = "Lỗi truy xuất lịch sử đơn hàng tại quầy" });
            }
        }
    }
}
