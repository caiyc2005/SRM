
using backend.Models.Dto;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class DeliveryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeliveryController(AppDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// 分页查询送货单列表（含明细，内存分页兼容低版本SQL）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetList([FromBody] DeliveryGetDto deliveryGetDto)
        {
            var query = _context.DeliveryNotes.Where(d => !d.IsDel).AsQueryable();

            if (!string.IsNullOrWhiteSpace(deliveryGetDto.noteCode))
                query = query.Where(d => d.NoteCode.Contains(deliveryGetDto.noteCode));
            if (!string.IsNullOrWhiteSpace(deliveryGetDto.supplierId))
                query = query.Where(d => d.SupplierID == deliveryGetDto.supplierId);
            if (deliveryGetDto.status.HasValue)
                query = query.Where(d => d.Status == deliveryGetDto.status.Value);

            var total = await query.CountAsync();

            // ✅ 修正：使用实体真实字段名 — DeliveryDetailID 和 MaterialName 等
            var allItems = await query
                .OrderByDescending(d => d.CreatedTime)
                .Select(d => new
                {
                    d.NoteID,
                    d.NoteCode,
                    d.OrderID,
                    d.SupplierID,
                    d.SupplierName,
                    d.Status,
                    d.ExpectedDate,
                    d.DeliveryDate,
                    d.CreateByName,
                    d.CreatedTime,
                    Details = d.DeliveryDetails
                        .Where(dd => !dd.IsDel)  // ✅ 加上软删除过滤（推荐）
                        .Select(dd => new
                        {
                            DetailID = dd.DeliveryDetailID,   // ✅ 正确：映射到 DeliveryDetailID
                            dd.MaterialCode,
                            MaterialName = dd.MaterialName ?? string.Empty, // 防 null
                            Unit = dd.Unit ?? string.Empty,
                            dd.Quantity,
                            dd.ReceivedQty,
                            // ❌ Remark 不存在，已移除；如需备注，请添加字段或用其他字段替代
                        })
                        .ToList()
                })
                .ToListAsync();

            var pagedItems = allItems
                .Skip((deliveryGetDto.page - 1) * deliveryGetDto.pageSize)
                .Take(deliveryGetDto.pageSize)
                .ToList();

            return Ok(new
            {
                code = 200,
                data = new
                {
                    items = pagedItems,
                    total,
                    page = deliveryGetDto.page,
                    pageSize = deliveryGetDto.pageSize
                }
            });
        }

        #region
        /*
        /// <summary>
        /// 获取送货单详情（含明细）
        /// </summary>
        [HttpGet("{noteId}")]
        public async Task<IActionResult> GetDetail(string notecode)
        {
            var note = await _context.DeliveryNotes
                .FirstOrDefaultAsync(n => n.NoteCode == notecode && !n.IsDel);
            if (note == null)
                return NotFound(new { code = 404, msg = "送货单不存在" });

            var details = await _context.DeliveryDetails
                .Where(d => note.NoteID == notecode && !d.IsDel)
                .Select(d => new
                {
                    d.DeliveryDetailID,
                    d.MaterialCode,
                    d.MaterialName,
                    d.Quantity,
                    d.Unit,
                    d.UnitPrice,
                    d.Amount,
                    d.ReceivedQty
                })
                .ToListAsync();

            return Ok(new { code = 200, data = new { note, details } });
        }
        */
        #endregion
        /// <summary>
        /// 基于采购订单生成送货单
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] DeliveryDto request)
        {
            // ✅ 安全处理可空 Status 比较
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(o => o.OrderID == request.OrderID && !o.IsDel);
            if (order == null)
                return BadRequest(new { code = 400, msg = "采购订单不存在或已被删除" });
            if (order.Status is not 1)
                return BadRequest(new { code = 400, msg = "仅允许已确认状态的采购订单生成送货单" });

            // 校验唯一性
            var exists = await _context.DeliveryNotes
                .AnyAsync(n => n.OrderID == request.OrderID && !n.IsDel);
            if (exists)
                return BadRequest(new { code = 400, msg = "该采购订单已生成送货单，不可重复生成" });

            // ✅ OrderDetail 已包含 IsDel 字段，可直接过滤
            var orderDetails = await _context.OrderDetails
                .Where(od => od.OrderID == request.OrderID )//&& !od.IsDel)
                .ToListAsync();
            if (!orderDetails.Any())
                return BadRequest(new { code = 400, msg = "采购订单无有效明细，无法生成送货单" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 生成单号
                var today = DateTime.Now.ToString("yyyyMMdd");
                var lastNote = await _context.DeliveryNotes
                    .Where(n => n.NoteCode.StartsWith($"DSH{today}"))
                    .OrderByDescending(n => n.NoteCode)
                    .FirstOrDefaultAsync();
                var seq = 1;
                if (lastNote != null && lastNote.NoteCode.Length >= 14 &&
                    int.TryParse(lastNote.NoteCode.Substring(11), out var lastSeq))
                    seq = lastSeq + 1;
                var noteCode = $"DSH{today}{seq:D3}";

                // 创建主表
                var deliveryNote = new DeliveryNote
                {
                    NoteID = Guid.NewGuid().ToString("N"),
                    NoteCode = noteCode,
                    OrderID = order.OrderID,
                    SupplierID = order.SupplierID ?? string.Empty,
                    SupplierName = order.SupplierName ?? string.Empty,
                    Status = false,
                    ExpectedDate = request.ExpectedDate,
                    CreateByID = request.CreateByID ?? string.Empty,
                    CreateByName = request.CreateByName ?? string.Empty,
                    CreatedTime = DateTime.Now,
                    IsDel = false,
                    UpdatedTime = null
                };
                _context.DeliveryNotes.Add(deliveryNote);

                // ✅ 优先使用 DTO 传入的数据，降级使用 OrderDetail 冗余字段
                foreach (var od in orderDetails)
                {
                    // 从 DTO 中查找当前物料的自定义配置
                    var dtoDetail = request.DetailQuantities?
                        .FirstOrDefault(q => q.MaterialCode == od.MaterialCode);

                    var customQty = dtoDetail?.Quantity ?? od.Qty;

                    if (customQty <= 0)
                        return BadRequest(new { code = 400, msg = $"物料 {od.MaterialCode} 的送货数量必须大于0" });

                    var detail = new DeliveryDetail
                    {
                        DeliveryDetailID = Guid.NewGuid().ToString("N"),
                        NoteID = deliveryNote.NoteID,
                        MaterialCode = od.MaterialCode,

                        // ✅ 优先级：DTO传入 > OrderDetail冗余字段 > 空字符串
                        MaterialName = dtoDetail.MaterialName,
                        Unit = dtoDetail.Unit,
                        Quantity = customQty,
                        UnitPrice = od.UnitPrice,
                        ReceivedQty = 0,
                        Amount = od.UnitPrice.HasValue ? od.UnitPrice.Value * customQty : null,
                        IsDel = false,
                        UpdatedTime = null
                    };

                    _context.DeliveryDetails.Add(detail);
                }

                // 更新订单状态为"已生成送货单"
                order.Status = 3;
                order.UpdateByID = request.CreateByID ?? string.Empty;
                order.UpdateByName = request.CreateByName ?? string.Empty;
                order.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { code = 200, msg = "送货单生成成功", data = new { deliveryNote.NoteID, deliveryNote.NoteCode } });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { code = 500, msg = $"生成失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 逻辑删除/作废送货单
        /// </summary>
        [HttpDelete("{noteId}")]
        public async Task<IActionResult> Delete(string noteId, [FromQuery] string operatorId, [FromQuery] string operatorName)
        {
            var note = await _context.DeliveryNotes
                .FirstOrDefaultAsync(n => n.NoteID == noteId && !n.IsDel);
            if (note == null)
                return NotFound(new { code = 404, msg = "送货单不存在" });
            if (note.Status)
                return BadRequest(new { code = 400, msg = "已收货的送货单不可作废" });

            note.IsDel = true;
            note.UpdatedTime = DateTime.Now;

            // ✅ DeliveryDetail 已包含 IsDel/UpdatedTime，可安全赋值
            var details = await _context.DeliveryDetails
                .Where(d => d.NoteID == noteId && !d.IsDel).ToListAsync();
            details.ForEach(d => { d.IsDel = true; d.UpdatedTime = DateTime.Now; });

            await _context.SaveChangesAsync();
            return Ok(new { code = 200, msg = "送货单已作废" });
        }
    }
}