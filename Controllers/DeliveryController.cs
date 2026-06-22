
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
        public async Task<IActionResult> GetDeliveryNote([FromBody] DeliveryGetDto deliveryGetDto)
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

        /// <summary>
        /// 根据采购订单生成送货单
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDeliveryNote([FromBody] DeliveryDto deliveryDto)
        {
            if (string.IsNullOrWhiteSpace(deliveryDto.OrderID))
                return BadRequest(new { code = 400, message = "采购订单ID不能为空" });

            if (string.IsNullOrWhiteSpace(deliveryDto.CreateByID))
                return BadRequest(new { code = 400, message = "创建人ID不能为空" });

            if (string.IsNullOrWhiteSpace(deliveryDto.CreateByName))
                return BadRequest(new { code = 400, message = "创建人姓名不能为空" });

            var purchaseOrder = await _context.PurchaseOrders
                .Include(o => o.OrderDetails)
                .Include(o => o.Supplier)
                .FirstOrDefaultAsync(o => o.OrderID == deliveryDto.OrderID && !o.IsDel);

            if (purchaseOrder == null)
                return NotFound(new { code = 404, message = "采购订单不存在" });

            if (purchaseOrder.Status != 1 && purchaseOrder.Status != 2)
                return BadRequest(new { code = 400, message = "采购订单状态不允许生成送货单" });

            var dateStr = DateTime.Now.ToString("yyyyMMdd");
            var todayMaxCode = await _context.DeliveryNotes
                .Where(d => d.NoteCode.StartsWith($"DSH{dateStr}") && !d.IsDel)
                .OrderByDescending(d => d.NoteCode)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (todayMaxCode != null)
            {
                var lastNumStr = todayMaxCode.NoteCode.Substring(11);
                if (int.TryParse(lastNumStr, out int lastNum))
                {
                    sequence = lastNum + 1;
                }
            }

            var noteCode = $"DSH{dateStr}{sequence.ToString("D3")}";

            var deliveryNote = new DeliveryNote
            {
                NoteID = Guid.NewGuid().ToString(),
                NoteCode = noteCode,
                OrderID = deliveryDto.OrderID,
                SupplierID = purchaseOrder.SupplierID,
                SupplierName = purchaseOrder.SupplierName,
                Status = false,
                ExpectedDate = deliveryDto.ExpectedDate,
                DeliveryDate = null,
                CreateByID = deliveryDto.CreateByID,
                CreateByName = deliveryDto.CreateByName,
                CreatedTime = DateTime.Now,
                IsDel = false
            };

            _context.DeliveryNotes.Add(deliveryNote);

            var orderDetailDict = purchaseOrder.OrderDetails?.ToDictionary(od => od.MaterialCode) ?? new Dictionary<string, OrderDetail>();

            Dictionary<string, Material> materialDict = new Dictionary<string, Material>();
            if (orderDetailDict.Any())
            {
                var materialCodes = orderDetailDict.Keys.ToList();
                materialDict = await _context.Materials
                    .Where(m => materialCodes.Contains(m.MaterialCode))
                    .ToDictionaryAsync(m => m.MaterialCode);
            }

            List<DeliveryDetail> deliveryDetails;
            if (deliveryDto.DetailQuantities != null && deliveryDto.DetailQuantities.Any())
            {
                deliveryDetails = new List<DeliveryDetail>();
                foreach (var item in deliveryDto.DetailQuantities)
                {
                    if (string.IsNullOrWhiteSpace(item.MaterialCode))
                        return BadRequest(new { code = 400, message = "物料编码不能为空" });

                    if (item.Quantity <= 0)
                        return BadRequest(new { code = 400, message = "送货数量必须大于0" });

                    if (!orderDetailDict.TryGetValue(item.MaterialCode, out var orderDetail))
                        return BadRequest(new { code = 400, message = $"采购订单中不存在物料：{item.MaterialCode}" });

                    if (item.Quantity > orderDetail.Qty)
                        return BadRequest(new { code = 400, message = $"送货数量超过采购数量，物料：{item.MaterialCode}" });

                    string materialName = item.MaterialName ?? materialDict?.GetValueOrDefault(item.MaterialCode)?.MaterialName ?? string.Empty;
                    string unit = item.Unit ?? materialDict?.GetValueOrDefault(item.MaterialCode)?.Unit ?? string.Empty;

                    deliveryDetails.Add(new DeliveryDetail
                    {
                        DeliveryDetailID = Guid.NewGuid().ToString(),
                        NoteID = deliveryNote.NoteID,
                        MaterialCode = item.MaterialCode,
                        MaterialName = materialName,
                        Unit = unit,
                        Quantity = item.Quantity,
                        UnitPrice = orderDetail.UnitPrice,
                        Amount = item.Quantity * (orderDetail.UnitPrice ?? 0),
                        ReceivedQty = 0,
                        IsDel = false,
                    });
                }
            }
            else
            {
                deliveryDetails = purchaseOrder.OrderDetails?
                    .Select(od => new DeliveryDetail
                    {
                        DeliveryDetailID = Guid.NewGuid().ToString(),
                        NoteID = deliveryNote.NoteID,
                        MaterialCode = od.MaterialCode,
                        MaterialName = materialDict?.GetValueOrDefault(od.MaterialCode)?.MaterialName ?? string.Empty,
                        Unit = materialDict?.GetValueOrDefault(od.MaterialCode)?.Unit ?? string.Empty,
                        Quantity = od.Qty,
                        UnitPrice = od.UnitPrice,
                        Amount = od.Amount,
                        ReceivedQty = 0,
                        IsDel = false,
                    })
                    .ToList() ?? new List<DeliveryDetail>();
            }

            _context.DeliveryDetails.AddRange(deliveryDetails);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = 200,
                message = "送货单创建成功",
                data = new
                {
                    deliveryNote.NoteID,
                    deliveryNote.NoteCode,
                    deliveryNote.OrderID,
                    deliveryNote.SupplierID,
                    deliveryNote.SupplierName,
                    deliveryNote.Status,
                    deliveryNote.ExpectedDate,
                    deliveryNote.CreatedTime,
                    DetailCount = deliveryDetails.Count
                }
            });
        }

        /// <summary>
        /// 删除送货单（软删除）
        /// </summary>
        [HttpDelete("{noteId}")]
        public async Task<IActionResult> DeleteDeliveryNote(string noteId)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                return BadRequest(new { code = 400, message = "送货单ID不能为空" });

            var deliveryNote = await _context.DeliveryNotes
                .Include(d => d.ReceiveRecords)
                .FirstOrDefaultAsync(d => d.NoteID == noteId && !d.IsDel);

            if (deliveryNote == null)
                return NotFound(new { code = 404, message = "送货单不存在" });

            if (deliveryNote.ReceiveRecords != null && deliveryNote.ReceiveRecords.Any())
                return BadRequest(new { code = 400, message = "该送货单已有收料记录，无法删除" });

            deliveryNote.IsDel = true;

            var details = await _context.DeliveryDetails
                .Where(dd => dd.NoteID == noteId)
                .ToListAsync();
            foreach (var detail in details)
            {
                detail.IsDel = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { code = 200, message = "送货单已删除" });
        }
    }
}