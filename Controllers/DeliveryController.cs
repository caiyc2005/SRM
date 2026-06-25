using backend.Models.Dto;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DeliveryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeliveryController(AppDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// 根据订单明细ID列表创建送货单（支持跨采购订单合并送货）
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,supplier")]
        public async Task<IActionResult> CreateDeliveryNote(DeliveryDto deliveryDto)
        {
            // ========== 参数校验 ==========
            if (deliveryDto.OrderDetailIDs == null || deliveryDto.OrderDetailIDs.Count == 0)
                return BadRequest(new { code = 400, message = "订单明细ID不能为空" });

            if (string.IsNullOrWhiteSpace(deliveryDto.CreateByID))
                return BadRequest(new { code = 400, message = "创建人ID不能为空" });

            if (string.IsNullOrWhiteSpace(deliveryDto.CreateByName))
                return BadRequest(new { code = 400, message = "创建人姓名不能为空" });

            // ========== 加载所有订单明细及对应采购订单 ==========
            var orderDetails = await _context.OrderDetails
                .Include(od => od.PurchaseOrder)
                .Where(od => deliveryDto.OrderDetailIDs.Contains(od.OrderDetailID))
                .ToListAsync();

            if (orderDetails.Count != deliveryDto.OrderDetailIDs.Count)
            {
                var foundIds = orderDetails.Select(od => od.OrderDetailID).ToHashSet();
                var missingIds = deliveryDto.OrderDetailIDs.Where(id => !foundIds.Contains(id)).ToList();
                return BadRequest(new { code = 400, message = $"以下订单明细不存在：{string.Join(", ", missingIds)}" });
            }

            // ========== 校验：所有明细必须属于同一个供应商 ==========
            var distinctSupplierIDs = orderDetails
                .Select(od => od.PurchaseOrder.SupplierID)
                .Distinct()
                .ToList();

            if (distinctSupplierIDs.Count > 1)
                return BadRequest(new { code = 400, message = "不能同时为不同供应商的订单创建送货单" });

            // ========== 校验：各采购订单状态是否允许生成送货单 ==========
            var orders = orderDetails
                .Select(od => od.PurchaseOrder)
                .DistinctBy(o => o.OrderID)
                .ToList();

            foreach (var order in orders)
            {
                if (order.IsDel)
                    return BadRequest(new { code = 400, message = $"采购订单({order.OrderCode})已被删除" });

                if (order.Status != 1 && order.Status != 2)
                    return BadRequest(new { code = 400, message = $"采购订单({order.OrderCode})状态不允许生成送货单" });
            }

            // ========== 供应商权限校验：只能给自己创建送货单 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserID == currentUserId);
                if (currentSupplier != null && currentSupplier.SupplierID != distinctSupplierIDs[0])
                    return BadRequest(new { code = 400, message = "只能给自己（当前供应商）创建送货单" });
            }

            // ========== 生成送货单号 ==========
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
                    sequence = lastNum + 1;
            }
            var noteCode = $"DSH{dateStr}{sequence.ToString("D3")}";

            // ========== 创建送货单（OrderID 取第一个采购订单，保持外键兼容） ==========
            var firstOrder = orders[0];
            var deliveryNote = new DeliveryNote
            {
                NoteID = Guid.NewGuid().ToString(),
                NoteCode = noteCode,
                OrderID = firstOrder.OrderID,
                SupplierID = distinctSupplierIDs[0],
                SupplierName = firstOrder.SupplierName,
                Status = false,
                ExpectedDate = deliveryDto.ExpectedDate,
                DeliveryDate = null,
                CreateByID = deliveryDto.CreateByID,
                CreateByName = deliveryDto.CreateByName,
                CreatedTime = DateTime.Now,
                IsDel = false
            };

            _context.DeliveryNotes.Add(deliveryNote);

            // ========== 构建物料字典用于校验 ==========
            var allOrderDetailsDict = orderDetails.ToDictionary(od => od.MaterialCode);

            // ========== 加载物料信息 ==========
            var materialCodes = orderDetails.Select(od => od.MaterialCode).Distinct().ToList();
            var materialDict = await _context.Materials
                .Where(m => materialCodes.Contains(m.MaterialCode))
                .ToDictionaryAsync(m => m.MaterialCode);

            // ========== 生成送货明细 ==========
            List<DeliveryDetail> deliveryDetails;
            if (deliveryDto.DetailQuantities != null && deliveryDto.DetailQuantities.Any())
            {
                // 前端传入了明细配置（支持指定数量、覆盖物料名称等）
                deliveryDetails = new List<DeliveryDetail>();
                foreach (var item in deliveryDto.DetailQuantities)
                {
                    if (string.IsNullOrWhiteSpace(item.MaterialCode))
                        return BadRequest(new { code = 400, message = "物料编码不能为空" });

                    if (item.Quantity <= 0)
                        return BadRequest(new { code = 400, message = "送货数量必须大于0" });

                    if (!allOrderDetailsDict.TryGetValue(item.MaterialCode, out var orderDetail))
                        return BadRequest(new { code = 400, message = $"所选订单明细中不存在物料：{item.MaterialCode}" });

                    if (item.Quantity > orderDetail.Qty)
                        return BadRequest(new { code = 400, message = $"送货数量超过采购数量，物料：{item.MaterialCode}" });

                    string materialName = item.MaterialName
                        ?? materialDict?.GetValueOrDefault(item.MaterialCode)?.MaterialName
                        ?? string.Empty;
                    string unit = item.Unit
                        ?? materialDict?.GetValueOrDefault(item.MaterialCode)?.Unit
                        ?? string.Empty;

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
                        IsDel = false
                    });
                }
            }
            else
            {
                // 前端未传明细配置，按所选 OrderDetail 的全量生成
                deliveryDetails = orderDetails
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
                        IsDel = false
                    })
                    .ToList();
            }

            _context.DeliveryDetails.AddRange(deliveryDetails);

            // ========== 更新涉及到的采购订单状态（1→2 待发货） ==========
            var ordersToUpdate = orders.Where(o => o.Status == 1).ToList();
            foreach (var order in ordersToUpdate)
            {
                order.Status = 2;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = 200,
                message = "送货单创建成功",
                data = new
                {
                    deliveryNote.NoteID,
                    deliveryNote.NoteCode,
                    PrimaryOrderID = deliveryNote.OrderID,
                    InvolvedOrders = orders.Select(o => new { o.OrderID, o.OrderCode }).ToList(),
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
        /// 供应商确认发货
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,supplier")]
        public async Task<IActionResult> DeliveryConfirm(ConfirmDeliveryDto confirmDto)
        {
            if (string.IsNullOrWhiteSpace(confirmDto.OrderID))
                return BadRequest(new { code = 400, message = "采购订单ID不能为空" });

            if (string.IsNullOrWhiteSpace(confirmDto.SupplierID))
                return BadRequest(new { code = 400, message = "供应商ID不能为空" });

            var purchaseOrder = await _context.PurchaseOrders
                .Include(o => o.Supplier)//连接供应商表
                .Include(o => o.DeliveryNotes)//连接送货单表
                .FirstOrDefaultAsync(o => o.OrderID == confirmDto.OrderID && !o.IsDel);

            if (purchaseOrder == null)
                return NotFound(new { code = 404, message = "采购订单不存在" });

            if (purchaseOrder.SupplierID != confirmDto.SupplierID)
                return BadRequest(new { code = 400, message = "供应商与订单不匹配" });

            // ========== 供应商权限校验：只能确认自己的发货 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserID == currentUserId);
                if (currentSupplier != null && currentSupplier.SupplierID != confirmDto.SupplierID)
                    return BadRequest(new { code = 400, message = "只能确认当前供应商的发货" });
            }

            if (purchaseOrder.Status != 2)
            {
                string statusMsg = purchaseOrder.Status switch
                {
                    0 => "订单待确认，无需发货",
                    1 => "订单已确认，请等待发货通知",
                    3 => "订单已完成发货，无需重复操作",
                    _ => "订单状态不允许发货"
                };
                return BadRequest(new { code = 400, message = statusMsg });
            }

            purchaseOrder.Status = 3;
            purchaseOrder.UpdateTime = DateTime.Now;

            foreach (var deliveryNote in purchaseOrder.DeliveryNotes?.Where(d => !d.IsDel && !d.Status) ?? new List<DeliveryNote>())
            {
                deliveryNote.DeliveryDate = DateTime.Now;
                if (confirmDto.ExpectedDeliveryDate.HasValue)
                {
                    deliveryNote.ExpectedDate = confirmDto.ExpectedDeliveryDate;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = 200,
                message = "发货确认成功",
                data = new
                {
                    purchaseOrder.OrderID,
                    purchaseOrder.OrderCode,
                    purchaseOrder.Status,
                    purchaseOrder.UpdateTime
                }
            });
        }

        /// <summary>
        /// 删除送货单（软删除）
        /// </summary>
        [HttpDelete("{noteId}")]
        [Authorize(Roles = "admin,supplier")]
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
            deliveryNote.UpdatedTime = DateTime.Now;

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

        /// <summary>
        /// 分页查询送货单列表（含明细，内存分页兼容低版本SQL）
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,supplier,whclerk")]
        public async Task<IActionResult> GetDeliveryNote(DeliveryGetDto deliveryGetDto)
        {
            var query = _context.DeliveryNotes.Where(d => !d.IsDel).AsQueryable();

            if (!string.IsNullOrWhiteSpace(deliveryGetDto.noteCode))
                query = query.Where(d => d.NoteCode.Contains(deliveryGetDto.noteCode));
            if (!string.IsNullOrWhiteSpace(deliveryGetDto.supplierId))
                query = query.Where(d => d.SupplierID == deliveryGetDto.supplierId);
            if (!string.IsNullOrWhiteSpace(deliveryGetDto.orderCode))
                query = query.Where(d => d.PurchaseOrder.OrderCode.Contains(deliveryGetDto.orderCode));
            if (deliveryGetDto.status.HasValue)
                query = query.Where(d => d.Status == deliveryGetDto.status.Value);

            // ========== 供应商权限校验：只能查看自己的送货单 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserID == currentUserId);
                if (currentSupplier != null)
                {
                    Console.WriteLine($"当前供应商{currentSupplier.SupplierID}，筛选其送货单数据");
                    query = query.Where(s => s.SupplierID == currentSupplier.SupplierID);
                }
            }

            var allItems = await query
                .OrderByDescending(d => d.CreatedTime)
                .Select(d => new
                {
                    d.NoteID,
                    d.NoteCode,
                    d.OrderID,
                    OrderCode = d.PurchaseOrder.OrderCode,
                    d.SupplierID,
                    d.SupplierName,
                    SupplierCode=d.Supplier.SupplierCode,
                    d.Status,
                    OrderStatus = d.PurchaseOrder.Status,//返回订单状态
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
                            Spec = _context.Materials.Where(m => m.MaterialCode == dd.MaterialCode).Select(m => m.Spec).FirstOrDefault() ?? string.Empty,
                            MaterialName = dd.MaterialName ?? string.Empty, // 防 null
                            Unit = dd.Unit ?? string.Empty,
                            dd.Quantity,
                            dd.ReceivedQty,
                            
                            unitPrice = dd.UnitPrice,
                            amount = dd.Amount,

                        })
                        .ToList()
                })
                .ToListAsync();

            var pagedItems = allItems
                .Skip((deliveryGetDto.page - 1) * deliveryGetDto.pageSize)
                .Take(deliveryGetDto.pageSize)
                .ToList();

            var total = allItems.Count;

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

        
    }
}