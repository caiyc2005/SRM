using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取库存列表（含物料、仓库信息）
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> GetAllFromInventory()
        {
            var list = await _context.Inventories
                .Include(i => i.Material)
                .Include(i => i.Warehouse)
                .Where(i => i.Qty > 0)
                .OrderByDescending(i => i.LastReceiveTime)
                .Select(i => new
                {
                    i.InventoryID,
                    i.MaterialID,
                    MaterialCode = i.Material.MaterialCode,
                    MaterialName = i.Material.MaterialName,
                    Spec = i.Material.Spec,
                    Unit = i.Material.Unit,
                    i.WareID,
                    WareCode = i.Warehouse.WareCode,
                    WareName = i.Warehouse.Name,
                    i.Qty,
                    i.LastReceiveTime,
                    i.UpdateByName,
                    i.UpdateByID
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", list));
        }
    }
}
