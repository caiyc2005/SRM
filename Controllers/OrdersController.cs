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
        /// <param name="orderCode">订单编号（模糊匹配）</param>
        /// <param name="supplierID">供应商ID（精确匹配）</param>
        /// <param name="status">订单状态：0-待确认，1-已确认，2-待发货，3-已发货，4-已收货</param>
        /// <param name="pageIndex">页码，默认1</param>
        /// <param name="pageSize">每页条数，默认20</param>
        /// <returns>分页订单列表</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetOrdersByList(
            [FromQuery] string? orderCode = null,
            [FromQuery] string? supplierID = null,
            [FromQuery] int? status = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            // ========== 构建查询条件 ==========
            var query = _context.PurchaseOrders
                .Where(o => !o.IsDel);

            // 根据订单编号模糊查询
            if (!string.IsNullOrWhiteSpace(orderCode))
                query = query.Where(o => o.OrderCode.Contains(orderCode));

            // 根据供应商ID精确查询
            if (!string.IsNullOrWhiteSpace(supplierID))
                query = query.Where(o => o.SupplierID == supplierID);

            // 根据状态筛选
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            // ========== 执行分页查询 ==========
            var total = await query.CountAsync();
            // 先查询所有数据到内存，再进行分页（兼容低版本SQL Server）
            // 修改查询，使用左连接
            var orders = await query
                .Include(o => o.Supplier)
                .Include(o => o.CreateByUser)
                .Include(o => o.OrderDetails)
        
                .OrderByDescending(o => o.CreateTime)
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
                        d.Amount
                    }).ToList()
                })
                .ToListAsync();
            // 在内存中进行分页和状态名称转换
            var paginatedOrders = orders
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
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
                    // 订单明细列表
                    OrderDetails = o.OrderDetails.Select(d => new
                    {
                        d.OrderDetailID,
                        d.MaterialCode,
                        d.MaterialName,
                        d.Spec,
                        d.Unit,
                        d.Qty,
                        d.UnitPrice,
                        d.Amount
                    }).ToList()
                })
                .ToList();

            return Ok(ApiResult.Ok("查询成功", new
            {
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize,
                List = paginatedOrders
            }));
        }

        /// <summary>
        /// 确认采购订单
        /// </summary>
        /// <param name="request">包含订单ID的请求对象</param>
        /// <returns>确认结果（订单ID、订单编号、新状态）</returns>
        /// <remarks>
        /// 将订单状态从"待确认"(0)变更为"已确认"(1)，只有状态为待确认的订单才能被确认。
        /// 确认后订单将进入待发货流程，供应商可以查看并安排发货。
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> ConfirmOrder(OrderIdRequest request)
        {
            // ========== 参数校验 ==========
            if (string.IsNullOrWhiteSpace(request.orderID))
                return BadRequest(ApiResult.Fail("订单ID不能为空"));

            // ========== 查询订单信息 ==========
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(o => o.OrderID == request.orderID && !o.IsDel);

            if (order == null)
                return NotFound(ApiResult.Fail("订单不存在"));

            // ========== 状态校验 ==========
            if (order.Status != 0)
                return BadRequest(ApiResult.Fail("订单状态不允许确认"));

            // ========== 获取当前用户信息 ==========
            //var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            //if (string.IsNullOrWhiteSpace(currentUserId))
            //    return Unauthorized(ApiResult.Fail("无法获取当前用户信息"));
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;

            // ========== 更新订单状态 ==========
            order.Status = 1;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                order.UpdateByID = currentUserId;
                order.UpdateByName = currentUserName;
            }
            order.UpdateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            // ========== 返回结果 ==========
            return Ok(ApiResult.Ok("订单确认成功", new
            {
                order.OrderID,
                order.OrderCode,
                order.Status,
                StatusName = GetStatusName(order.Status)
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
