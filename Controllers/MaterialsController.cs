using backend.Models;
using backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin")]
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

        /// <summary>
        /// 新增物料
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> CreateMaterial([FromBody] CreateMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MaterialCode))
                return BadRequest(ApiResult.Fail("物料编码不能为空"));

            if (string.IsNullOrWhiteSpace(dto.MaterialName))
                return BadRequest(ApiResult.Fail("物料名称不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Spec))
                return BadRequest(ApiResult.Fail("规格不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Unit))
                return BadRequest(ApiResult.Fail("单位不能为空"));

            var exists = await _context.Materials.AnyAsync(m => m.MaterialCode == dto.MaterialCode && !m.IsDel);
            if (exists)
                return BadRequest(ApiResult.Fail("物料编码已存在"));

            var material = new Material
            {
                MaterialID = Guid.NewGuid().ToString(),
                MaterialCode = dto.MaterialCode,
                MaterialName = dto.MaterialName,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Memo = dto.Memo,
                IsDel = false
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("物料新增成功", new MaterialDto
            {
                MaterialID = material.MaterialID,
                MaterialCode = material.MaterialCode,
                MaterialName = material.MaterialName,
                Spec = material.Spec,
                Unit = material.Unit,
                Memo = material.Memo
            }));
        }

        /// <summary>
        /// 修改物料
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> UpdateMaterial([FromBody] UpdateMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MaterialID))
                return BadRequest(ApiResult.Fail("物料ID不能为空"));

            var material = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialID == dto.MaterialID && !m.IsDel);
            if (material == null)
                return NotFound(ApiResult.Fail("物料不存在"));

            if (string.IsNullOrWhiteSpace(dto.MaterialCode))
                return BadRequest(ApiResult.Fail("物料编码不能为空"));

            if (string.IsNullOrWhiteSpace(dto.MaterialName))
                return BadRequest(ApiResult.Fail("物料名称不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Spec))
                return BadRequest(ApiResult.Fail("规格不能为空"));

            if (string.IsNullOrWhiteSpace(dto.Unit))
                return BadRequest(ApiResult.Fail("单位不能为空"));

            var exists = await _context.Materials.AnyAsync(m => m.MaterialCode == dto.MaterialCode && m.MaterialID != dto.MaterialID && !m.IsDel);
            if (exists)
                return BadRequest(ApiResult.Fail("物料编码已被其他物料使用"));

            material.MaterialCode = dto.MaterialCode;
            material.MaterialName = dto.MaterialName;
            material.Spec = dto.Spec;
            material.Unit = dto.Unit;
            material.Memo = dto.Memo;

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("物料修改成功", new MaterialDto
            {
                MaterialID = material.MaterialID,
                MaterialCode = material.MaterialCode,
                MaterialName = material.MaterialName,
                Spec = material.Spec,
                Unit = material.Unit,
                Memo = material.Memo
            }));
        }

        /// <summary>
        /// 删除物料（软删除）
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> DeleteMaterial(DeleteMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MaterialID))
                return BadRequest(ApiResult.Fail("物料ID不能为空"));

            var material = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialID == dto.MaterialID && !m.IsDel);
            if (material == null)
                return NotFound(ApiResult.Fail("物料不存在"));

            material.IsDel = true;
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("物料已删除"));
        }

        

    }
}