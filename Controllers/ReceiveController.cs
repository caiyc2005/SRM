using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    
    public class ReceiveController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReceiveController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = "admin,whclerk")]
        public async Task<IActionResult> CreateReceive([FromBody] ReceiveCreateDto receiveCreateDto)
        {

            var ware = await _context.Warehouses.Where(w => w.WareID == receiveCreateDto.wareID && !w.IsDel).FirstOrDefaultAsync();
            Console.WriteLine($"获取到的仓库ID：{receiveCreateDto.wareID}");
            if (ware == null)
            {
                return BadRequest(new { code = 400, message = "仓库已被禁用，不可收料" });
            }

            if (string.IsNullOrWhiteSpace(receiveCreateDto.NoteCode))
                return BadRequest(new { code = 400, message = "送货单号不能为空" });

            if (string.IsNullOrWhiteSpace(receiveCreateDto.ReceiveUserID))
                return BadRequest(new { code = 400, message = "收料人ID不能为空" });

            if (string.IsNullOrWhiteSpace(receiveCreateDto.ReceiveUserName))
                return BadRequest(new { code = 400, message = "收料人姓名不能为空" });

            var deliveryNote = await _context.DeliveryNotes
                .Include(d => d.DeliveryDetails)
                    .ThenInclude(dd => dd.OrderDetail)
                        .ThenInclude(od => od.PurchaseOrder)
                .Include(d => d.Supplier)
                .FirstOrDefaultAsync(d => d.NoteCode == receiveCreateDto.NoteCode && !d.IsDel);

            if (deliveryNote == null)
                return NotFound(new { code = 404, message = "送货单不存在" });

            if (deliveryNote.Status)
                return BadRequest(new { code = 400, message = "该送货单已完成收料，不允许重复收料" });

            var receiveUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == receiveCreateDto.ReceiveUserID && !u.IsDel);

            if (receiveUser == null)
                return BadRequest(new { code = 400, message = "收料人不存在" });

            var materialCodes = deliveryNote.DeliveryDetails
                .Where(dd => !dd.IsDel)
                .Select(dd => dd.MaterialCode)
                .Distinct()
                .ToList();

            var materialDict = await _context.Materials
                .Where(m => materialCodes.Contains(m.MaterialCode) && !m.IsDel)
                .ToDictionaryAsync(m => m.MaterialCode, m => m.MaterialID);

            var dateStr = DateTime.Now.ToString("yyyyMMdd");

            var todayMaxCode = await _context.ReceiveRecords
                .Where(r => r.ReceiveCode.StartsWith($"SL{dateStr}") && !r.IsDel)
                .OrderByDescending(r => r.ReceiveCode)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (todayMaxCode != null)
            {
                var lastNumStr = todayMaxCode.ReceiveCode.Substring(8);
                if (int.TryParse(lastNumStr, out int lastNum))
                {
                    sequence = lastNum + 1;
                }
            }

            var receiveCode = $"SL{dateStr}{sequence.ToString("D3")}";

            var receiveRecord = new ReceiveRecord
            {
                ReceiveID = Guid.NewGuid().ToString(),
                ReceiveCode = receiveCode,
                NoteID = deliveryNote.NoteID,
                SupplierID = deliveryNote.SupplierID,
                SupplierName = deliveryNote.SupplierName,
                ReceiveUserID = receiveCreateDto.ReceiveUserID,
                ReceiveUserName = receiveCreateDto.ReceiveUserName,
                ReceiveDate = DateTime.Now,
                IsDel = false,
                Memo = receiveCreateDto.Memo
            };

            _context.ReceiveRecords.Add(receiveRecord);

            var deliveryDetailDict = deliveryNote.DeliveryDetails
                .Where(dd => !dd.IsDel)
                .GroupBy(dd => dd.MaterialCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<ReceiveDetail> receiveDetails;
            var warnings = new List<string>();
            if (receiveCreateDto.Details != null && receiveCreateDto.Details.Any())
            {
                receiveDetails = new List<ReceiveDetail>();
                foreach (var item in receiveCreateDto.Details)
                {
                    if (string.IsNullOrWhiteSpace(item.MaterialCode))
                        return BadRequest(new { code = 400, message = "物料编码不能为空" });

                    if (item.ReceivedQty < 0)
                        return BadRequest(new { code = 400, message = "收料数量不可小于0" });

                    if (!deliveryDetailDict.TryGetValue(item.MaterialCode, out var deliveryDetails))
                        return BadRequest(new { code = 400, message = $"送货单中不存在物料：{item.MaterialCode}" });

                    if (!materialDict.TryGetValue(item.MaterialCode, out var materialId))
                        return BadRequest(new { code = 400, message = $"物料不存在：{item.MaterialCode}" });

                    var deliveryDetail = deliveryDetails.FirstOrDefault(dd => dd.ReceivedQty <= dd.Quantity);
                    if (deliveryDetail == null)
                        return BadRequest(new { code = 400, message = $"物料 {item.MaterialCode} 已全部完成收料，不可重复收料" });

                    var remainingQty = deliveryDetail.Quantity - deliveryDetail.ReceivedQty;
                    if (item.ReceivedQty > remainingQty)
                        return BadRequest(new { code = 400, message = $"收料数量不能超过送货单剩余数量（最大 {remainingQty}），物料：{item.MaterialCode}" });

                    if (item.ReceivedQty < deliveryDetail.Quantity / 2)
                        warnings.Add($"物料 {item.MaterialCode} 实收数量（{item.ReceivedQty}）少于送货数量的一半（{deliveryDetail.Quantity}），请确认");

                    receiveDetails.Add(new ReceiveDetail
                    {
                        ReceiveDetailID = Guid.NewGuid().ToString(),
                        ReceiveID = receiveRecord.ReceiveID,
                        DeliveryDetailID = deliveryDetail.DeliveryDetailID,
                        MaterialID = materialId,
                        MaterialCode = item.MaterialCode,
                        PlanQty = deliveryDetail.Quantity,
                        ReceivedQty = item.ReceivedQty,
                        DiffQty = deliveryDetail.Quantity - item.ReceivedQty,
                        CreateBy = receiveCreateDto.ReceiveUserID,
                        CreateTime = DateTime.Now,
                        IsDel = false
                    });

                    deliveryDetail.ReceivedQty += item.ReceivedQty;
                }
            }
            else
            {
                receiveDetails = deliveryNote.DeliveryDetails
                    .Where(dd => !dd.IsDel)
                    .Select(dd => new ReceiveDetail
                    {
                        ReceiveDetailID = Guid.NewGuid().ToString(),
                        ReceiveID = receiveRecord.ReceiveID,
                        DeliveryDetailID = dd.DeliveryDetailID,
                        MaterialID = materialDict.TryGetValue(dd.MaterialCode, out var mid) ? mid : string.Empty,
                        MaterialCode = dd.MaterialCode,
                        PlanQty = dd.Quantity,
                        ReceivedQty = dd.Quantity - dd.ReceivedQty,
                        DiffQty = 0,
                        CreateBy = receiveCreateDto.ReceiveUserID,
                        CreateTime = DateTime.Now,
                        IsDel = false
                    })
                    .ToList();

                foreach (var dd in deliveryNote.DeliveryDetails.Where(dd => !dd.IsDel))
                {
                    dd.ReceivedQty = dd.Quantity;
                }
            }

            _context.ReceiveDetails.AddRange(receiveDetails);

            bool isFullyReceived = deliveryNote.DeliveryDetails.All(dd => dd.IsDel || dd.ReceivedQty >= dd.Quantity);
            if (isFullyReceived)
            {
                deliveryNote.Status = true;
                deliveryNote.DeliveryDate = DateTime.Now;
            }

            // 全部收料 → 更新关联采购订单状态为 4（已收货）
            if (isFullyReceived)
            {
                var orders = deliveryNote.DeliveryDetails
                    .Where(dd => !dd.IsDel && dd.OrderDetail?.PurchaseOrder != null)
                    .Select(dd => dd.OrderDetail.PurchaseOrder)
                    .DistinctBy(o => o.OrderID)
                    .ToList();

                foreach (var order in orders)
                {
                    order.Status = 4;
                    order.UpdateByID = receiveCreateDto.ReceiveUserID;
                    order.UpdateByName = receiveCreateDto.ReceiveUserName;
                    order.UpdateTime = DateTime.Now;
                }
            }

            foreach (var detail in receiveDetails)
            {

                //var newInventory = new Inventory
                //{
                //    InventoryID = Guid.NewGuid().ToString(),
                //    MaterialID = detail.MaterialID,
                //    WareID = receiveCreateDto.wareID,
                //    Qty = detail.ReceivedQty,
                //    LastReceiveTime = DateTime.Now,
                //    UpdateByID = receiveCreateDto.ReceiveUserID,
                //    UpdateByName = receiveCreateDto.ReceiveUserName
                //};
                //_context.Inventories.Add(newInventory);

                var existingInventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.MaterialID == detail.MaterialID && i.WareID==receiveCreateDto.wareID);//&& !i.WareID.Contains("TEMP"));

                if (existingInventory != null)
                {
                    existingInventory.Qty += detail.ReceivedQty;
                    existingInventory.LastReceiveTime = DateTime.Now;
                    existingInventory.UpdateByID = receiveCreateDto.ReceiveUserID;
                    existingInventory.UpdateByName = receiveCreateDto.ReceiveUserName;
                }
                else
                {
                    var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => !w.IsDel && w.WareID == receiveCreateDto.wareID);

                    if (warehouse != null)
                    {
                        var newInventory = new Inventory
                        {
                            InventoryID = Guid.NewGuid().ToString(),
                            MaterialID = detail.MaterialID,
                            WareID = warehouse.WareID,
                            Qty = detail.ReceivedQty,
                            LastReceiveTime = DateTime.Now,
                            UpdateByID = receiveCreateDto.ReceiveUserID,
                            UpdateByName = receiveCreateDto.ReceiveUserName
                        };
                        _context.Inventories.Add(newInventory);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = 200,
                message = "收料单创建成功",
                data = new
                {
                    receiveRecord.ReceiveID,
                    receiveRecord.ReceiveCode,
                    receiveRecord.NoteID,
                    receiveRecord.SupplierID,
                    receiveRecord.SupplierName,
                    receiveRecord.ReceiveUserID,
                    receiveRecord.ReceiveUserName,
                    receiveRecord.ReceiveDate,
                    receiveRecord.Memo,
                    DetailCount = receiveDetails.Count,
                    IsFullyReceived = isFullyReceived,
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            });
        }

        [HttpPost]
        [Authorize(Roles = "admin,whclerk")]
        public async Task<IActionResult> GetReceivesList([FromBody] ReceiveGetDto receiveGetDto)
        {
            var query = _context.ReceiveRecords.Where(r => !r.IsDel).AsQueryable();

            if (!string.IsNullOrWhiteSpace(receiveGetDto.ReceiveCode))
                query = query.Where(r => r.ReceiveCode.Contains(receiveGetDto.ReceiveCode));

            if (!string.IsNullOrWhiteSpace(receiveGetDto.NoteCode))
                query = query.Where(r => r.DeliveryNote.NoteCode.Contains(receiveGetDto.NoteCode));

            if (!string.IsNullOrWhiteSpace(receiveGetDto.SupplierId))
                query = query.Where(r => r.SupplierID == receiveGetDto.SupplierId);

            var total = await query.CountAsync();

            var allItems = await query
                .OrderByDescending(r => r.ReceiveDate)
                .Select(r => new
                {
                    r.ReceiveID,
                    r.ReceiveCode,
                    r.NoteID,
                    NoteCode = r.DeliveryNote.NoteCode,
                    OrderCodes = r.DeliveryNote.DeliveryDetails
                        .Where(dd => !dd.IsDel && dd.OrderDetail != null)
                        .Select(dd => dd.OrderDetail.PurchaseOrder.OrderCode)
                        .Distinct()
                        .ToList(),
                    r.SupplierID,
                    r.SupplierName,
                    r.ReceiveUserID,
                    r.ReceiveUserName,
                    r.ReceiveDate,
                    r.Memo,
                    Details = r.ReceiveDetails
                        .Where(rd => !rd.IsDel)
                        .Select(rd => new
                        {
                            rd.ReceiveDetailID,
                            rd.MaterialCode,
                            Spec = rd.Material.Spec ?? string.Empty,
                            MaterialName = rd.DeliveryDetail.MaterialName ?? string.Empty,
                            rd.PlanQty,
                            rd.ReceivedQty,
                            rd.DiffQty,
                            Unit = rd.DeliveryDetail.Unit ?? string.Empty,
                            UnitPrice = rd.DeliveryDetail.UnitPrice,
                            Amount = rd.ReceivedQty * (rd.DeliveryDetail.UnitPrice ?? 0)
                        })
                        .ToList()
                })
                .ToListAsync();

            var pagedItems = allItems
                .Skip((receiveGetDto.page - 1) * receiveGetDto.pageSize)
                .Take(receiveGetDto.pageSize)
                .ToList();

            return Ok(new
            {
                code = 200,
                data = new
                {
                    items = pagedItems,
                    total,
                    page = receiveGetDto.page,
                    pageSize = receiveGetDto.pageSize
                }
            });
        }
    }
}