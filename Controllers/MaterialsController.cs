using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有物料列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetAll()
        {
            var materials = await _context.Materials
                .Where(m => !m.IsDel)
                .OrderBy(m => m.MaterialCode)
                .Select(m => new MaterialDto
                {
                    MaterialID = m.MaterialID,
                    MaterialCode = m.MaterialCode,
                    MaterialName = m.MaterialName,
                    Spec = m.Spec,
                    Unit = m.Unit,
                    Memo = m.Memo
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", materials));
        }
    }
}
