using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReceiveController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const decimal ERROR_TOLERANCE = 0.05m;

        public ReceiveController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReceive([FromBody] ReceiveCreateDto receiveCreateDto)
        {
            if (string.IsNullOrWhiteSpace(receiveCreateDto.NoteCode))
                return BadRequest(new { code = 400, message = "送货单号不能为空" });

            if (string.IsNullOrWhiteSpace(receiveCreateDto.ReceiveUserID))
                return BadRequest(new { code = 400, message = "收料人ID不能为空" });

            if (string.IsNullOrWhiteSpace(receiveCreateDto.ReceiveUserName))
                return BadRequest(new { code = 400, message = "收料人姓名不能为空" });

            var deliveryNote = await _context.DeliveryNotes
                .Include(d => d.DeliveryDetails)
                .Include(d => d.Supplier)
                .Include(d => d.PurchaseOrder)
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
            if (receiveCreateDto.Details != null && receiveCreateDto.Details.Any())
            {
                receiveDetails = new List<ReceiveDetail>();
                foreach (var item in receiveCreateDto.Details)
                {
                    if (string.IsNullOrWhiteSpace(item.MaterialCode))
                        return BadRequest(new { code = 400, message = "物料编码不能为空" });

                    if (item.ReceivedQty <= 0)
                        return BadRequest(new { code = 400, message = "收料数量必须大于0" });

                    if (!deliveryDetailDict.TryGetValue(item.MaterialCode, out var deliveryDetails))
                        return BadRequest(new { code = 400, message = $"送货单中不存在物料：{item.MaterialCode}" });

                    if (!materialDict.TryGetValue(item.MaterialCode, out var materialId))
                        return BadRequest(new { code = 400, message = $"物料不存在：{item.MaterialCode}" });

                    var deliveryDetail = deliveryDetails.First(dd => dd.ReceivedQty < dd.Quantity);
                    if (deliveryDetail == null)
                        return BadRequest(new { code = 400, message = $"物料 {item.MaterialCode} 已全部收料完成" });

                    var availableQty = deliveryDetail.Quantity - deliveryDetail.ReceivedQty;
                    var maxAllowedQty = availableQty * (1 + ERROR_TOLERANCE);
                    var minAllowedQty = availableQty * (1 - ERROR_TOLERANCE);

                    if (item.ReceivedQty > maxAllowedQty)
                        return BadRequest(new { code = 400, message = $"收料数量超过允许范围（最大允许 {maxAllowedQty:F4}），物料：{item.MaterialCode}" });

                    if (item.ReceivedQty < minAllowedQty && availableQty > 0)
                        return BadRequest(new { code = 400, message = $"收料数量低于允许范围（最小允许 {minAllowedQty:F4}），物料：{item.MaterialCode}" });

                    receiveDetails.Add(new ReceiveDetail
                    {
                        ReceiveDetailID = Guid.NewGuid().ToString(),
                        ReceiveID = receiveRecord.ReceiveID,
                        DeliveryDetailID = deliveryDetail.DeliveryDetailID,
                        MaterialID = materialId,
                        MaterialCode = deliveryDetail.MaterialCode,
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

            if (isFullyReceived && deliveryNote.PurchaseOrder != null)
            {
                deliveryNote.PurchaseOrder.Status = 3;
                deliveryNote.PurchaseOrder.UpdateByID = receiveCreateDto.ReceiveUserID;
                deliveryNote.PurchaseOrder.UpdateByName = receiveCreateDto.ReceiveUserName;
                deliveryNote.PurchaseOrder.UpdateTime = DateTime.Now;
            }

            foreach (var detail in receiveDetails)
            {
                var existingInventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.MaterialID == detail.MaterialID && !i.WareID.Contains("TEMP"));

                if (existingInventory != null)
                {
                    existingInventory.Qty += detail.ReceivedQty;
                    existingInventory.LastReceiveTime = DateTime.Now;
                    existingInventory.UpdateByID = receiveCreateDto.ReceiveUserID;
                    existingInventory.UpdateByName = receiveCreateDto.ReceiveUserName;
                }
                else
                {
                    var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => !w.IsDel);
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
                    IsFullyReceived = isFullyReceived
                }
            });
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetReceive([FromBody] ReceiveGetDto receiveGetDto)
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