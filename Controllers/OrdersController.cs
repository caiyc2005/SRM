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
    [Authorize]
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
        public async Task<ActionResult<ApiResult>> CreateOrder([FromBody] OrderCreateDto dto)
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
            var dateStr = DateTime.Now.ToString("yyyyMMddHHmmss");
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
    }
}
