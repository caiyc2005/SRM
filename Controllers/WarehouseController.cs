using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WarehouseController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有仓库列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetAllWarehouse()
        {
            var list = await _context.Warehouses
                //.Where(w => !w.IsDel)
                .OrderBy(w => w.WareCode)
                .Select(w => new WarehouseDto
                {
                    WareID = w.WareID,
                    WareCode = w.WareCode,
                    Name = w.Name,
                    Address = w.Address,
                    Memo = w.Memo,
                    CreateTime = w.CreateTime,
                    IsDel = w.IsDel
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", list));
        }

        /// <summary>
        /// 新增仓库
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> CreateWarehouse([FromBody] CreateWarehouseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.WareCode))
                return BadRequest(ApiResult.Fail("仓库编码不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(ApiResult.Fail("仓库名称不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest(ApiResult.Fail("仓库地址不能为空"));

            var exists = await _context.Warehouses.AnyAsync(w => w.WareCode == dto.WareCode);//&& !w.IsDel);
            if (exists)
                return BadRequest(ApiResult.Fail("仓库编码已存在"));

            var warehouse = new Warehouse
            {
                WareID = Guid.NewGuid().ToString(),
                WareCode = dto.WareCode,
                Name = dto.Name,
                Address = dto.Address,
                Memo = dto.Memo,
                CreateTime = DateTime.Now,
                IsDel = false
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("仓库新增成功", new WarehouseDto
            {
                WareID = warehouse.WareID,
                WareCode = warehouse.WareCode,
                Name = warehouse.Name,
                Address = warehouse.Address,
                Memo = warehouse.Memo,
                CreateTime = warehouse.CreateTime
            }));
        }

        /// <summary>
        /// 修改仓库
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> UpdateWarehouse([FromBody] UpdateWarehouseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.WareID))
                return BadRequest(ApiResult.Fail("仓库ID不能为空"));

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.WareID == dto.WareID && !w.IsDel);
            if (warehouse == null)
                return NotFound(ApiResult.Fail("仓库不存在"));

            if (string.IsNullOrWhiteSpace(dto.WareCode))
                return BadRequest(ApiResult.Fail("仓库编码不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(ApiResult.Fail("仓库名称不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest(ApiResult.Fail("仓库地址不能为空"));

            var exists = await _context.Warehouses.AnyAsync(w => w.WareCode == dto.WareCode && w.WareID != dto.WareID && !w.IsDel);
            if (exists)
                return BadRequest(ApiResult.Fail("仓库编码已被其他仓库使用"));

            warehouse.WareCode = dto.WareCode;
            warehouse.Name = dto.Name;
            warehouse.Address = dto.Address;
            warehouse.Memo = dto.Memo;

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("仓库修改成功", new WarehouseDto
            {
                WareID = warehouse.WareID,
                WareCode = warehouse.WareCode,
                Name = warehouse.Name,
                Address = warehouse.Address,
                Memo = warehouse.Memo,
                CreateTime = warehouse.CreateTime
            }));
        }

        /// <summary>
        /// 启用或禁用仓库
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> SetWarehouseStatus([FromBody] SetWarehouseStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.WareID))
                return BadRequest(ApiResult.Fail("仓库ID不能为空"));

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.WareID == dto.WareID);
            if (warehouse == null)
                return NotFound(ApiResult.Fail("仓库不存在"));

            warehouse.IsDel = !dto.IsEnable;

            await _context.SaveChangesAsync();

            var msg = dto.IsEnable ? "仓库已启用" : "仓库已禁用";
            return Ok(ApiResult.Ok(msg));
        }
    }
}
