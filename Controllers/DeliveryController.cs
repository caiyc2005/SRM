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
        public async Task<IActionResult> CreateDeliveryNote([FromBody] DeliveryDto deliveryDto)
        {
            // ========== 参数校验 ==========
            if (deliveryDto.Items == null || deliveryDto.Items.Count == 0)
                return BadRequest(new { code = 400, message = "送货明细不能为空" });

            if (string.IsNullOrWhiteSpace(deliveryDto.CreateByID))
                return BadRequest(new { code = 400, message = "创建人ID不能为空" });

            if (string.IsNullOrWhiteSpace(deliveryDto.CreateByName))
                return BadRequest(new { code = 400, message = "创建人姓名不能为空" });

            // ========== 提取所有 OrderDetailID ==========
            var orderDetailIDs = deliveryDto.Items.Select(i => i.OrderDetailID).Distinct().ToList();
            if (orderDetailIDs.Any(string.IsNullOrWhiteSpace))
                return BadRequest(new { code = 400, message = "订单明细ID不能为空" });

            // ========== 加载所有订单明细 ==========
            var orderDetails = await _context.OrderDetails
                .Include(od => od.PurchaseOrder)
                .Include(od => od.Material)
                .Where(od => orderDetailIDs.Contains(od.OrderDetailID))
                .ToListAsync();

            if (orderDetails.Count != orderDetailIDs.Count)
            {
                var foundIds = orderDetails.Select(od => od.OrderDetailID).ToHashSet();
                var missingIds = orderDetailIDs.Where(id => !foundIds.Contains(id)).ToList();
                return BadRequest(new { code = 400, message = $"以下订单明细不存在：{string.Join(", ", missingIds)}" });
            }

            // ========== 校验：所有明细必须属于同一个供应商 ==========
            var distinctSupplierIDs = orderDetails
                .Select(od => od.PurchaseOrder.SupplierID)
                .Distinct()
                .ToList();

            if (distinctSupplierIDs.Count > 1)
                return BadRequest(new { code = 400, message = "不能同时为不同供应商的订单创建送货单" });

            // ========== 校验：各采购订单状态 ==========
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

            // ========== 供应商权限校验 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.SupplierUsers
                    .FirstOrDefaultAsync(su => su.UserID == currentUserId);
                if (currentSupplier != null && currentSupplier.SupplierID != distinctSupplierIDs[0])
                    return BadRequest(new { code = 400, message = "只能给自己（当前供应商）创建送货单" });
            }

            // ========== 校验：各明细的送货数量（含已有送货，累计不超采购总数） ==========
            var orderDetailDict = orderDetails.ToDictionary(od => od.OrderDetailID);

            // 获取各明细已有的累计送货数量
            var existingDelivered = await _context.DeliveryDetails
                .Where(dd => !dd.IsDel && orderDetailIDs.Contains(dd.OrderDetailID))
                .GroupBy(dd => dd.OrderDetailID)
                .Select(g => new { OrderDetailID = g.Key, TotalQty = g.Sum(dd => dd.Quantity) })
                .ToDictionaryAsync(g => g.OrderDetailID, g => g.TotalQty);

            foreach (var item in deliveryDto.Items)
            {
                if (item.DeliveryQty <= 0)
                    return BadRequest(new { code = 400, message = $"送货数量必须大于0，明细：{item.OrderDetailID}" });

                if (!orderDetailDict.TryGetValue(item.OrderDetailID, out var od))
                    return BadRequest(new { code = 400, message = $"所选订单明细中不存在：{item.OrderDetailID}" });

                var alreadyQty = existingDelivered.GetValueOrDefault(item.OrderDetailID, 0);
                var remainQty = od.Qty - alreadyQty;
                if (item.DeliveryQty > remainQty)
                {
                    return BadRequest(new { code = 400, message = $"累计送货数量超出采购数量（采购 {od.Qty}，已送 {alreadyQty}，可送 {remainQty}），明细：{item.OrderDetailID}" });
                }
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

            // ========== 创建送货单 ==========
            var primaryOrder = orders[0];
            var deliveryNote = new DeliveryNote
            {
                NoteID = Guid.NewGuid().ToString(),
                NoteCode = noteCode,
                SupplierID = distinctSupplierIDs[0],
                SupplierName = primaryOrder.SupplierName,
                Status = 0,
                ExpectedDate = deliveryDto.ExpectedDate,
                DeliveryDate = null,
                CreateByID = deliveryDto.CreateByID,
                CreateByName = deliveryDto.CreateByName,
                CreatedTime = DateTime.Now,
                IsDel = false
            };
            _context.DeliveryNotes.Add(deliveryNote);

            // ========== 生成送货明细 ==========
            var deliveryDetails = deliveryDto.Items
                .Select(item =>
                {
                    var od = orderDetailDict[item.OrderDetailID];
                    return new DeliveryDetail
                    {
                        DeliveryDetailID = Guid.NewGuid().ToString(),
                        NoteID = deliveryNote.NoteID,
                        OrderDetailID = od.OrderDetailID,
                        MaterialCode = od.Material?.MaterialCode ?? string.Empty,
                        MaterialName = od.Material?.MaterialName ?? string.Empty,
                        Unit = od.Material?.Unit ?? string.Empty,
                        Quantity = item.DeliveryQty,
                        UnitPrice = od.UnitPrice,
                        Amount = item.DeliveryQty * (od.UnitPrice ?? 0),
                        ReceivedQty = 0,
                        IsDel = false
                    };
                })
                .ToList();

            _context.DeliveryDetails.AddRange(deliveryDetails);

            // ========== 更新订单明细状态（累计已送数量 ≥ 采购数量时才设为 2=已生成送货单） ==========
            // 本次各明细的送货数量
            var currentDeliveredQty = deliveryDetails
                .GroupBy(dd => dd.OrderDetailID)
                .ToDictionary(g => g.Key, g => g.Sum(dd => dd.Quantity));

            // 3. 判断是否全部送齐
            foreach (var od in orderDetails)
            {
                var alreadyDelivered = existingDelivered.GetValueOrDefault(od.OrderDetailID, 0);
                var currentDeliver = currentDeliveredQty.GetValueOrDefault(od.OrderDetailID, 0);
                if (alreadyDelivered + currentDeliver >= od.Qty)
                {
                    od.IsConfirm = 2;
                }
            }

            // ========== 更新涉及到的采购订单状态（全部明细已生成送货单时才设为 2 待发货） ==========
            foreach (var order in orders)
            {
                if (order.Status >= 2) continue;

                var allDetails = await _context.OrderDetails
                    .Where(od => od.OrderID == order.OrderID)
                    .ToListAsync();

                if (allDetails.All(od => od.IsConfirm >= 2))
                {
                    order.Status = 2;
                    order.UpdateTime = DateTime.Now;
                }
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
                    //PrimaryOrderID = deliveryNote.OrderID,
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
        /// 供应商确认发货 — 根据送货单ID，更新对应订单明细为已发货，同订单全部发货时更新主档状态
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,supplier")]
        public async Task<IActionResult> DeliveryConfirm(ConfirmDeliveryDto confirmDto)
        {
            if (string.IsNullOrWhiteSpace(confirmDto.noteID))
                return BadRequest(new { code = 400, message = "送货单ID不能为空" });

            // ========== 加载送货单及明细 ==========
            var deliveryNote = await _context.DeliveryNotes
                .Include(d => d.DeliveryDetails)
                .FirstOrDefaultAsync(d => d.NoteID == confirmDto.noteID && !d.IsDel);

            if (deliveryNote == null)
                return NotFound(new { code = 404, message = "送货单不存在" });

            if (deliveryNote.Status >= 1)
                return BadRequest(new { code = 400, message = "该送货单已完成发货，无需重复操作" });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;

            // ========== 供应商权限校验 ==========
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.SupplierUsers
                    .FirstOrDefaultAsync(su => su.UserID == currentUserId);
                if (currentSupplier != null && currentSupplier.SupplierID != deliveryNote.SupplierID)
                    return BadRequest(new { code = 400, message = "只能确认当前供应商的发货" });
            }

            // ========== 获取该送货单涉及的所有 OrderDetailID ==========
            var orderDetailIDs = deliveryNote.DeliveryDetails
                .Where(dd => !dd.IsDel)
                .Select(dd => dd.OrderDetailID)
                .Distinct()
                .ToList();

            if (orderDetailIDs.Count == 0)
                return BadRequest(new { code = 400, message = "送货单没有有效的明细" });

            // ========== 加载对应的订单明细及采购订单 ==========
            var orderDetails = await _context.OrderDetails
                .Include(od => od.PurchaseOrder)
                .Where(od => orderDetailIDs.Contains(od.OrderDetailID))
                .ToListAsync();

            // ========== 更新订单明细 IsConfirm = 3（已发货），跳过已是 3 的 ==========
            var updatedCount = 0;
            foreach (var od in orderDetails)
            {
                if (od.IsConfirm < 3)
                {
                    od.IsConfirm = 3;
                    updatedCount++;
                }
            }

            // ========== 更新送货单状态 ==========
            deliveryNote.Status = 1;
            deliveryNote.DeliveryDate = DateTime.Now;
            if (confirmDto.ExpectedDeliveryDate.HasValue)
            {
                deliveryNote.ExpectedDate = confirmDto.ExpectedDeliveryDate;
            }

            // ========== 按采购订单分组，检查是否全部明细都已发货 ==========
            var orderGroups = orderDetails
                .Where(od => od.PurchaseOrder != null)
                .GroupBy(od => od.PurchaseOrder)
                .ToList();

            var updatedOrderIds = new List<string>();
            foreach (var group in orderGroups)
            {
                var order = group.Key;
                // 检查该采购订单的所有明细（不限于本次送货的）
                var allOrderDetails = await _context.OrderDetails
                    .Where(od => od.OrderID == order.OrderID)
                    .ToListAsync();

                bool allDelivered = allOrderDetails.All(od => od.IsConfirm >= 3);
                if (allDelivered && order.Status < 3)
                {
                    order.Status = 3;
                    order.UpdateTime = DateTime.Now;
                    order.UpdateByID = currentUserId;
                    order.UpdateByName = currentUserName;
                    updatedOrderIds.Add(order.OrderID);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = 200,
                message = "发货确认成功",
                data = new
                {
                    deliveryNote.NoteID,
                    deliveryNote.NoteCode,
                    deliveryNote.DeliveryDate,
                    DetailCount = orderDetailIDs.Count,
                    UpdatedDetailCount = updatedCount,
                    FullyDeliveredOrders = updatedOrderIds
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
                query = query.Where(d => d.DeliveryDetails.Any(dd => dd.OrderDetail.PurchaseOrder.OrderCode.Contains(deliveryGetDto.orderCode)));
            if (deliveryGetDto.status.HasValue)
                query = query.Where(d => d.Status == deliveryGetDto.status.Value);

            if (deliveryGetDto.isReceived == false)
                query = query.Where(d => d.Status != 2);

            // 按创建时间起始过滤
            if (deliveryGetDto.StartTime.HasValue)
                query = query.Where(d => d.CreatedTime >= deliveryGetDto.StartTime.Value);

            // 按创建时间截止过滤（包含当天最后一刻）
            if (deliveryGetDto.EndTime.HasValue)
                query = query.Where(d => d.CreatedTime <= deliveryGetDto.EndTime.Value.AddDays(1).AddTicks(-1));

            // 按发货时间起始过滤
            if (deliveryGetDto.DeliveryStartTime.HasValue)
                query = query.Where(d => d.DeliveryDate >= deliveryGetDto.DeliveryStartTime.Value);

            // 按发货时间截止过滤（包含当天最后一刻）
            if (deliveryGetDto.DeliveryEndTime.HasValue)
                query = query.Where(d => d.DeliveryDate <= deliveryGetDto.DeliveryEndTime.Value.AddDays(1).AddTicks(-1));

            // ========== 供应商权限校验：只能查看自己的送货单 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.SupplierUsers
                    .FirstOrDefaultAsync(su => su.UserID == currentUserId);
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
                    d.SupplierID,
                    d.SupplierName,
                    SupplierCode=d.Supplier.SupplierCode,
                    d.Status,
                    d.ExpectedDate,
                    d.DeliveryDate,
                    d.CreateByName,
                    d.CreatedTime,
                    
                    Details = d.DeliveryDetails
                        .Where(dd => !dd.IsDel)  // ✅ 加上软删除过滤（推荐）
                        .Select(dd => new
                        {
                            DeliveryDetailID = dd.DeliveryDetailID,
                            dd.MaterialCode,
                            Spec = _context.Materials.Where(m => m.MaterialCode == dd.MaterialCode).Select(m => m.Spec).FirstOrDefault() ?? string.Empty,
                            MaterialName = dd.MaterialName ?? string.Empty, // 防 null
                            Unit = dd.Unit ?? string.Empty,
                            dd.Quantity,
                            dd.ReceivedQty,

                            unitPrice = dd.UnitPrice,
                            amount = dd.Amount,

                            OrderCode = dd.OrderDetail.PurchaseOrder.OrderCode,
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


        [HttpPost]
        [Authorize(Roles = "admin,supplier,whclerk")]
        public async Task<IActionResult> GetDeliveryNoteNotIn(DeliveryGetDto2 deliveryGetDto)
        {
            var query = _context.DeliveryNotes.Where(d => !d.IsDel).AsQueryable();

            query = query.Where(d => d.Status != 2 && d.Status<=1);


            // ========== 供应商权限校验：只能查看自己的送货单 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var currentSupplier = await _context.SupplierUsers
                    .FirstOrDefaultAsync(su => su.UserID == currentUserId);
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
                    d.SupplierID,
                    d.SupplierName,
                    SupplierCode = d.Supplier.SupplierCode,
                    d.Status,
                    d.ExpectedDate,
                    d.DeliveryDate,
                    d.CreateByName,
                    d.CreatedTime,

                    Details = d.DeliveryDetails
                        .Where(dd => !dd.IsDel)  // ✅ 加上软删除过滤（推荐）
                        .Select(dd => new
                        {
                            DeliveryDetailID = dd.DeliveryDetailID,
                            dd.MaterialCode,
                            Spec = _context.Materials.Where(m => m.MaterialCode == dd.MaterialCode).Select(m => m.Spec).FirstOrDefault() ?? string.Empty,
                            MaterialName = dd.MaterialName ?? string.Empty, // 防 null
                            Unit = dd.Unit ?? string.Empty,
                            dd.Quantity,
                            dd.ReceivedQty,

                            unitPrice = dd.UnitPrice,
                            amount = dd.Amount,

                            OrderCode = dd.OrderDetail.PurchaseOrder.OrderCode,
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