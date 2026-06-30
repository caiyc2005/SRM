using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin,supplier,purchase")]
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
        public async Task<ActionResult<ApiResult>> CreateOrder(OrderDto dto)
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
                    MaterialID = material.MaterialID,

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
        /// 查询订单明细（关联采购订单和物料）
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> GetOrdersDetailsByList(OrderDetailsDto detailsDto)
        {
            // 从 OrderDetails 为主表，关联 PurchaseOrder 和 Material
            var queryable = _context.OrderDetails
                .Include(od => od.PurchaseOrder)
                .Include(od => od.Material)
                .Where(od => !od.PurchaseOrder.IsDel);

            // 按采购订单编号过滤
            if (!string.IsNullOrWhiteSpace(detailsDto.OrderCode))
                queryable = queryable.Where(od => od.PurchaseOrder.OrderCode.Contains(detailsDto.OrderCode));

            // 按供应商ID过滤
            if (!string.IsNullOrWhiteSpace(detailsDto.SupplierID))
                queryable = queryable.Where(od => od.PurchaseOrder.SupplierID == detailsDto.SupplierID);

            // 按订单明细状态过滤（0=未确认, 1=已确认, 2=送货中, 3=已发货）
            if (detailsDto.IsConfirm.HasValue)
                queryable = queryable.Where(od => od.IsConfirm == detailsDto.IsConfirm.Value);

            // 按采购订单创建时间起始过滤
            if (detailsDto.StartTime.HasValue)
                queryable = queryable.Where(od => od.PurchaseOrder.CreateTime >= detailsDto.StartTime.Value);

            // 按采购订单创建时间截止过滤（包含当天最后一刻）
            if (detailsDto.EndTime.HasValue)
                queryable = queryable.Where(od => od.PurchaseOrder.CreateTime <= detailsDto.EndTime.Value.AddDays(1).AddTicks(-1));

            // 统计总数
            var total = await queryable.CountAsync();

            // 分页查询
            var list = await queryable
                .OrderByDescending(od => od.PurchaseOrder.CreateTime)//先以创建时间排序desc
                .ThenBy(od => od.OrderDetailID)//再以订单明细ID排序
                .Skip((detailsDto.PageIndex - 1) * detailsDto.PageSize)
                .Take(detailsDto.PageSize)
                .Select(od => new
                {
                    od.OrderDetailID,
                    od.OrderID,
                    // 采购订单信息
                    OrderCode = od.PurchaseOrder.OrderCode,
                    SupplierID = od.PurchaseOrder.SupplierID,
                    SupplierName = od.PurchaseOrder.SupplierName,
                    OrderStatus = od.PurchaseOrder.Status,
                    //OrderStatusName = GetStatusName(od.PurchaseOrder.Status),
                    OrderCreateTime = od.PurchaseOrder.CreateTime,
                    // 物料信息
                    MaterialCode = od.Material.MaterialCode,
                    MaterialName = od.Material.MaterialName,
                    Spec = od.Material.Spec,
                    Unit = od.Material.Unit,
                    // 明细信息
                    od.Qty,
                    DeliveredQty = _context.DeliveryDetails
                        .Where(dd => !dd.IsDel && dd.OrderDetailID == od.OrderDetailID)
                        .Sum(dd => (decimal?)dd.Quantity) ?? 0m,
                    AvailableQty = od.Qty - (_context.DeliveryDetails
                        .Where(dd => !dd.IsDel && dd.OrderDetailID == od.OrderDetailID)
                        .Sum(dd => (decimal?)dd.Quantity) ?? 0m),
                    od.UnitPrice,
                    od.Amount,
                    od.IsConfirm
                })//子查询
                .ToListAsync();

            return Ok(ApiResult.Ok("查询订单明细成功", new
            {
                Total = total,
                PageIndex = detailsDto.PageIndex,
                PageSize = detailsDto.PageSize,
                List = list
            }));
        }



        [HttpPost]
        // 获取已确认的采购订单（只返回 IsConfirm >= 1 的明细）
        public async Task<ActionResult<ApiResult>> GetConfirmedOrders(OrderDetailsDto detailsDto)
        {
            // 从 OrderDetails 为主表，关联 PurchaseOrder 和 Material
            var queryable = _context.OrderDetails
                .Include(od => od.PurchaseOrder)
                .Include(od => od.Material)
                .Where(od => !od.PurchaseOrder.IsDel && od.IsConfirm == 1);

            // 按采购订单编号过滤
            if (!string.IsNullOrWhiteSpace(detailsDto.OrderCode))
                queryable = queryable.Where(od => od.PurchaseOrder.OrderCode.Contains(detailsDto.OrderCode));

            // 按供应商ID过滤
            if (!string.IsNullOrWhiteSpace(detailsDto.SupplierID))
                queryable = queryable.Where(od => od.PurchaseOrder.SupplierID == detailsDto.SupplierID);

            // 按订单明细状态过滤（IsConfirm: 0=未确认, 1=已确认, 2=送货中, 3=已发货）
            if (detailsDto.IsConfirm.HasValue)
                queryable = queryable.Where(od => od.IsConfirm == detailsDto.IsConfirm.Value);

            // ========== 供应商权限校验：供应商只能查看自己的订单 ==========
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var supplierUser = await _context.SupplierUsers
                    .FirstOrDefaultAsync(su => su.UserID == currentUserId);
                if (supplierUser != null)
                {
                    queryable = queryable.Where(od => od.PurchaseOrder.SupplierID == supplierUser.SupplierID);
                }
            }

            // 统计总数
            var total = await queryable.CountAsync();

            // 分页查询
            var list = await queryable
                .OrderByDescending(od => od.PurchaseOrder.CreateTime)//先以创建时间排序desc
                .ThenBy(od => od.OrderDetailID)//再以订单明细ID排序
                .Skip((detailsDto.PageIndex - 1) * detailsDto.PageSize)
                .Take(detailsDto.PageSize)
                .Select(od => new
                {
                    od.OrderDetailID,
                    od.OrderID,
                    // 采购订单信息
                    OrderCode = od.PurchaseOrder.OrderCode,
                    SupplierID = od.PurchaseOrder.SupplierID,
                    SupplierName = od.PurchaseOrder.SupplierName,
                    OrderStatus = od.PurchaseOrder.Status,
                    //OrderStatusName = GetStatusName(od.PurchaseOrder.Status),
                    OrderCreateTime = od.PurchaseOrder.CreateTime,
                    // 物料信息
                    MaterialCode = od.Material.MaterialCode,
                    MaterialName = od.Material.MaterialName,
                    Spec = od.Material.Spec,
                    Unit = od.Material.Unit,
                    // 明细信息
                    od.Qty,
                    DeliveredQty = _context.DeliveryDetails
                        .Where(dd => !dd.IsDel && dd.OrderDetailID == od.OrderDetailID)
                        .Sum(dd => (decimal?)dd.Quantity) ?? 0m,
                    AvailableQty = od.Qty - (_context.DeliveryDetails
                        .Where(dd => !dd.IsDel && dd.OrderDetailID == od.OrderDetailID)
                        .Sum(dd => (decimal?)dd.Quantity) ?? 0m),
                    od.UnitPrice,
                    od.Amount,
                    od.IsConfirm
                })//子查询
                .ToListAsync();

            return Ok(ApiResult.Ok("查询订单明细成功", new
            {
                Total = total,
                PageIndex = detailsDto.PageIndex,
                PageSize = detailsDto.PageSize,
                List = list
            }));
        }


        /// <summary>
        /// 采购订单查询
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>分页订单列表</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> GetOrdersByList(OrderQueryDto query)
        {
            #region （暂时没用）限制至少填写一个查询条件
            //bool hasCondition = !string.IsNullOrWhiteSpace(query.OrderCode)
            //    || !string.IsNullOrWhiteSpace(query.SupplierID)
            //    || query.Status.HasValue
            //    || query.StartDate.HasValue
            //    || query.EndDate.HasValue;

            //if (!hasCondition)
            //    return BadRequest(ApiResult.Fail("至少填写一个查询条件"));
            #endregion

            var queryable = _context.PurchaseOrders
                .Where(o => !o.IsDel);

            if (!string.IsNullOrWhiteSpace(query.OrderCode))
                queryable = queryable.Where(o => o.OrderCode.Contains(query.OrderCode));

            if (!string.IsNullOrWhiteSpace(query.SupplierID))
                queryable = queryable.Where(o => o.SupplierID == query.SupplierID);

            if (query.Status.HasValue)
                queryable = queryable.Where(o => o.Status == query.Status.Value);

            if (query.StartTime.HasValue)
                queryable = queryable.Where(o => o.CreateTime >= query.StartTime.Value);
            Console.WriteLine("query.StartTime=========================" + query.StartTime);

            if (query.EndTime.HasValue)
                queryable = queryable.Where(o => o.CreateTime <= query.EndTime.Value.AddDays(1).AddTicks(-1));

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
                    NoteCode = _context.DeliveryDetails
                        .Where(dd => !dd.IsDel && dd.OrderDetail.OrderID == o.OrderID)
                        .Select(dd => dd.DeliveryNote.NoteCode)
                        .FirstOrDefault() ?? "",
                    OrderDetails = o.OrderDetails.Select(d => new
                    {
                        d.OrderDetailID,
                        d.MaterialID,
                        MaterialCode = d.Material.MaterialCode,
                        MaterialName = d.Material.MaterialName,
                        Spec = d.Material.Spec,
                        Unit = d.Material.Unit,
                        d.Qty,
                        DeliveredQty = _context.DeliveryDetails
                            .Where(dd => !dd.IsDel && dd.OrderDetailID == d.OrderDetailID)
                            .Sum(dd => (decimal?)dd.Quantity) ?? 0m,
                        AvailableQty = d.Qty - (_context.DeliveryDetails
                            .Where(dd => !dd.IsDel && dd.OrderDetailID == d.OrderDetailID)
                            .Sum(dd => (decimal?)dd.Quantity) ?? 0m),
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
                    //StatusName = GetStatusName(o.Status),
                    o.CreateByID,
                    o.CreateByName,
                    o.CreateTime,
                    o.UpdateTime,
                    o.Memo,
                    o.NoteCode,
                    OrderDetails = o.OrderDetails.Select(d => new
                    {
                        d.OrderDetailID,
                        d.MaterialID,
                        d.MaterialCode,
                        d.MaterialName,
                        d.Spec,
                        d.Unit,
                        d.Qty,
                        d.DeliveredQty,
                        d.AvailableQty,
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
        /// 整单确认 — 将当前采购订单下所有未确认的明细全部确认为已确认
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> ConfirmOrder([FromBody] OrderIdRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.orderID))
                return BadRequest(ApiResult.Fail("采购订单ID不能为空"));

            var order = await _context.PurchaseOrders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderID == dto.orderID && !o.IsDel);

            if (order == null)
                return NotFound(ApiResult.Fail("采购订单不存在"));

            if (order.Status >= 1)
                return BadRequest(ApiResult.Fail("该订单已完成确认，无需重复操作"));

            var unconfirmedDetails = order.OrderDetails?
                .Where(od => od.IsConfirm == 0)
                .ToList() ?? new List<OrderDetail>();

            if (unconfirmedDetails.Count == 0)
                return BadRequest(ApiResult.Fail("没有可确认的订单明细"));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;

            foreach (var detail in unconfirmedDetails)
            {
                detail.IsConfirm = 1;
            }

            order.Status = 1;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                order.UpdateByID = currentUserId;
                order.UpdateByName = currentUserName;
            }
            order.UpdateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("整单确认成功", new
            {
                order.OrderID,
                order.OrderCode,
                order.Status,
                ConfirmedCount = unconfirmedDetails.Count
            }));
        }

        /// <summary>
        /// 确认采购订单（支持按物料分批确认）
        /// </summary>
        /// <param name="request">确认请求（订单ID + 可选的物料编码列表）</param>
        /// <returns>确认结果</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> ConfirmOrderDetail(ConfirmOrderDetailDto dto)
        {
            if (dto.OrderDetailIDs == null || dto.OrderDetailIDs.Count == 0)
                return BadRequest(ApiResult.Fail("订单明细ID不能为空"));

            // 先查出本次请求要确认的明细（仅未确认的）
            var targetDetails = await _context.OrderDetails
                .Include(od => od.PurchaseOrder)
                .Where(od => dto.OrderDetailIDs.Contains(od.OrderDetailID) && od.IsConfirm == 0)
                .ToListAsync();

            if (targetDetails.Count == 0)
                return BadRequest(ApiResult.Fail("没有可确认的订单明细"));

            var orderIds = targetDetails.Select(od => od.OrderID).Distinct().ToList();
            if (orderIds.Count > 1)
                return BadRequest(ApiResult.Fail("一次只能确认同一个采购订单的明细"));

            var firstDetail = targetDetails.FirstOrDefault();
            if (firstDetail?.PurchaseOrder == null || firstDetail.PurchaseOrder.IsDel)
                return NotFound(ApiResult.Fail("订单不存在"));

            var orderId = firstDetail.OrderID;
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;

            // 重新加载采购订单及其下所有明细（确保检查完整性时不会遗漏）
            var order = await _context.PurchaseOrders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && !o.IsDel);

            if (order == null)
                return NotFound(ApiResult.Fail("订单不存在"));

            List<string> confirmedMaterialIDs = new List<string>();
            foreach (var detail in targetDetails)
            {
                // 从已加载的 order.OrderDetails 中找到对应实体做修改
                var od = order.OrderDetails.FirstOrDefault(x => x.OrderDetailID == detail.OrderDetailID);
                if (od != null && od.IsConfirm == 0)
                {
                    od.IsConfirm = 1;
                    confirmedMaterialIDs.Add(od.MaterialID);
                }
            }

            // 判断该采购订单下所有明细是否都已完成确认（基于已加载的完整集合，避免查数据库延迟不一致）
            bool allConfirmed = order.OrderDetails.All(od => od.IsConfirm >= 1);

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
                ConfirmedMaterialIDs = confirmedMaterialIDs,
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
