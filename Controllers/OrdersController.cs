using System.Security.Claims;
using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin,supplier")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 创建采购订单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> CreateOrder([FromBody] OrderDto dto)
        {
            // ========== 参数校验 ==========
            if (string.IsNullOrWhiteSpace(dto.SupplierID))
                return BadRequest(ApiResult.Fail("供应商ID不能为空"));

            if (dto.Materials == null || dto.Materials.Count == 0)
                return BadRequest(ApiResult.Fail("物料明细不能为空"));

            // 检查供应商是否存在
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierID);
            if (supplier == null)
                return NotFound(ApiResult.Fail("供应商不存在"));

            // ========== 从 JWT 获取当前用户 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ApiResult.Fail("无法获取当前用户信息"));

            // ========== 生成订单编号 ==========
            var dateStr = DateTime.Now.ToString("yyyyMMdd");
            var guidPart = Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper();
            //var orderCode = $"PO{dateStr}{new Random().Next(100, 999)}";//这个方法随机生成随机数，但是有概率会重复
            var orderCode = $"PO{dateStr}{guidPart}";

            // ========== 创建采购订单 ==========
            var order = new PurchaseOrder
            {
                OrderID = Guid.NewGuid().ToString(),
                OrderCode = orderCode,
                SupplierID = dto.SupplierID,
                SupplierName = dto.SupplierName ?? supplier.SupplierName,
                Status = 0, // 待确认
                CreateByID = currentUserId,
                CreateByName = currentUserName ?? "",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                IsDel = false,
                Memo = dto.Memo
            };

            _context.PurchaseOrders.Add(order);

            // ========== 创建订单明细 ==========
            var materialIds = dto.Materials.Select(m => m.MaterialID).ToList();
            var materials = await _context.Materials
                .Where(m => materialIds.Contains(m.MaterialID))
                .ToDictionaryAsync(m => m.MaterialID);

            foreach (var item in dto.Materials)
            {
                if (!materials.TryGetValue(item.MaterialID, out var material))
                    return BadRequest(ApiResult.Fail($"物料不存在：{item.MaterialID}"));

                var detail = new OrderDetail
                {
                    OrderDetailID = Guid.NewGuid().ToString(),
                    OrderID = order.OrderID,
                    MaterialCode = material.MaterialCode,
                    Qty = item.Qty,
                    UnitPrice = item.UnitPrice,
                    Amount = item.Qty * (item.UnitPrice ?? 0)
                };

                _context.OrderDetails.Add(detail);
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("采购订单创建成功", new
            {
                order.OrderID,
                order.OrderCode,
                order.Status,
                order.CreateTime
            }));
        }
        /// <summary>
        /// 采购订单查询
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>分页订单列表</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetOrdersByList([FromBody] OrderQueryDto query)
        {
            bool hasCondition = !string.IsNullOrWhiteSpace(query.OrderCode)
                || !string.IsNullOrWhiteSpace(query.SupplierID)
                || query.Status.HasValue
                || query.StartDate.HasValue
                || query.EndDate.HasValue;

            if (!hasCondition)
                return BadRequest(ApiResult.Fail("至少填写一个查询条件"));

            var queryable = _context.PurchaseOrders
                .Where(o => !o.IsDel);

            if (!string.IsNullOrWhiteSpace(query.OrderCode))
                queryable = queryable.Where(o => o.OrderCode.Contains(query.OrderCode));

            if (!string.IsNullOrWhiteSpace(query.SupplierID))
                queryable = queryable.Where(o => o.SupplierID == query.SupplierID);

            if (query.Status.HasValue)
                queryable = queryable.Where(o => o.Status == query.Status.Value);

            if (query.StartDate.HasValue)
                queryable = queryable.Where(o => o.CreateTime >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                queryable = queryable.Where(o => o.CreateTime <= query.EndDate.Value.AddDays(1).AddTicks(-1));

            var total = await queryable.CountAsync();

            var ordersQuery = queryable
                .Include(o => o.Supplier)
                .Include(o => o.CreateByUser)
                .Include(o => o.OrderDetails);

            IOrderedQueryable<PurchaseOrder> orderedQuery;
            switch ((query.SortField ?? "CreateTime").ToLower())
            {
                case "ordercode":
                    orderedQuery = query.SortOrder?.ToLower() == "asc"
                        ? ordersQuery.OrderBy(o => o.OrderCode)
                        : ordersQuery.OrderByDescending(o => o.OrderCode);
                    break;
                case "status":
                    orderedQuery = query.SortOrder?.ToLower() == "asc"
                        ? ordersQuery.OrderBy(o => o.Status)
                        : ordersQuery.OrderByDescending(o => o.Status);
                    break;
                case "createtime":
                default:
                    orderedQuery = query.SortOrder?.ToLower() == "asc"
                        ? ordersQuery.OrderBy(o => o.CreateTime)
                        : ordersQuery.OrderByDescending(o => o.CreateTime);
                    break;
            }

            var orders = await orderedQuery
                .Select(o => new
                {
                    o.OrderID,
                    o.OrderCode,
                    o.SupplierID,
                    o.SupplierName,
                    SupplierContact = o.Supplier.People,
                    SupplierPhone = o.Supplier.PhoneNumber,
                    o.Status,
                    o.CreateByID,
                    o.CreateByName,
                    o.CreateTime,
                    o.UpdateTime,
                    o.Memo,
                    NoteCode = o.DeliveryNotes.Where(dn => !dn.IsDel).Select(dn => dn.NoteCode).FirstOrDefault() ?? "",
                    OrderDetails = o.OrderDetails.Select(d => new
                    {
                        d.OrderDetailID,
                        d.MaterialCode,
                        MaterialName = _context.Materials.Where(m => m.MaterialCode == d.MaterialCode).Select(m => m.MaterialName).FirstOrDefault(),
                        Spec = _context.Materials.Where(m => m.MaterialCode == d.MaterialCode).Select(m => m.Spec).FirstOrDefault(),
                        Unit = _context.Materials.Where(m => m.MaterialCode == d.MaterialCode).Select(m => m.Unit).FirstOrDefault(),
                        d.Qty,
                        d.UnitPrice,
                        d.Amount,
                        d.IsConfirm
                    }).ToList()
                })
                .ToListAsync();

            var paginatedOrders = orders
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(o => new
                {
                    o.OrderID,
                    o.OrderCode,
                    o.SupplierID,
                    o.SupplierName,
                    o.SupplierContact,
                    o.SupplierPhone,
                    o.Status,
                    StatusName = GetStatusName(o.Status),
                    o.CreateByID,
                    o.CreateByName,
                    o.CreateTime,
                    o.UpdateTime,
                    o.Memo,
                    o.NoteCode,
                    OrderDetails = o.OrderDetails.Select(d => new
                    {
                        d.OrderDetailID,
                        d.MaterialCode,
                        d.MaterialName,
                        d.Spec,
                        d.Unit,
                        d.Qty,
                        d.UnitPrice,
                        d.Amount,
                        d.IsConfirm
                    }).ToList()
                })
                .ToList();

            return Ok(ApiResult.Ok("查询成功", new
            {
                Total = total,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                List = paginatedOrders
            }));
        }

        /// <summary>
        /// 确认采购订单（支持按物料分批确认）
        /// </summary>
        /// <param name="request">确认请求（订单ID + 可选的物料编码列表）</param>
        /// <returns>确认结果</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> ConfirmOrder([FromBody] ConfirmOrderDto request)
        {
            if (string.IsNullOrWhiteSpace(request.OrderID))
                return BadRequest(ApiResult.Fail("订单ID不能为空"));

            var order = await _context.PurchaseOrders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderID == request.OrderID && !o.IsDel);

            if (order == null)
                return NotFound(ApiResult.Fail("订单不存在"));

            if (order.Status != 0)
                return BadRequest(ApiResult.Fail("订单状态不允许确认"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;

            var orderDetails = order.OrderDetails.Where(od => !od.IsConfirm).ToList();

            List<string> confirmedMaterials = new List<string>();
            if (request.MaterialCodes != null && request.MaterialCodes.Any())
            {
                foreach (var materialCode in request.MaterialCodes)
                {
                    var detail = orderDetails.FirstOrDefault(od => od.MaterialCode == materialCode);
                    if (detail != null)
                    {
                        detail.IsConfirm = true;
                        confirmedMaterials.Add(materialCode);
                    }
                }
            }
            else
            {
                foreach (var detail in orderDetails)
                {
                    detail.IsConfirm = true;
                    confirmedMaterials.Add(detail.MaterialCode);
                }
            }

            bool allConfirmed = order.OrderDetails.All(od => od.IsConfirm);
            if (allConfirmed)
            {
                order.Status = 1;
                if (!string.IsNullOrWhiteSpace(currentUserId))
                {
                    order.UpdateByID = currentUserId;
                    order.UpdateByName = currentUserName;
                }
                order.UpdateTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok(allConfirmed ? "订单确认成功" : "部分物料确认成功", new
            {
                order.OrderID,
                order.OrderCode,
                order.Status,
                StatusName = GetStatusName(order.Status),
                ConfirmedMaterials = confirmedMaterials,
                IsAllConfirmed = allConfirmed
            }));
        }

        private static string GetStatusName(int status)
        {
            return status switch
            {
                0 => "待确认",
                1 => "已确认",
                2 => "待发货",
                3 => "已发货",
                4 => "已收货",
                _ => "未知"
            };
        }
    }

}
