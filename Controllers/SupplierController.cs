using Azure.Core;
using backend.Models;
using backend.Models.Dto;
using backend.Models.Dto.Supplier;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupplierController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 查询供应商信息表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> GetAllSuppliers()
        {
            var suppliers = await _context.Suppliers
                //.Where(r => !r.IsDel)
                .Select(r => new SupplierDto
                {
                    supplierID = r.SupplierID,
                    supplierCode = r.SupplierCode,
                    supplierName = r.SupplierName,
                    people = r.People,
                    phoneNumber = r.PhoneNumber,
                    address = r.Address,
                    isDel = r.IsDel,
                    memo = r.Memo

                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", suppliers));
        }


        [HttpPost]
        public async Task<ActionResult<ApiResult>> UpdateSupplier(UpdateSupplierDto updateSupplierDto)
        {
            // ========== 参数校验 ==========
            if (string.IsNullOrWhiteSpace(updateSupplierDto.supplierID))
                return BadRequest(ApiResult.Fail("供应商ID不能为空"));
            if (string.IsNullOrWhiteSpace(updateSupplierDto.supplierCode))
                return BadRequest(ApiResult.Fail("供应商编号不能为空"));
            if (string.IsNullOrWhiteSpace(updateSupplierDto.people))
                return BadRequest(ApiResult.Fail("供应商联系人不能为空"));
            if (string.IsNullOrWhiteSpace(updateSupplierDto.phoneNumber))
                return BadRequest(ApiResult.Fail("供应商的联系方式不能为空"));


            // ========== 查询供应商信息 ==========
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(o => o.SupplierID == updateSupplierDto.supplierID);//&& !o.IsDel);

            if (supplier == null)
                return NotFound(ApiResult.Fail("供应商不存在"));

            // ========== 状态校验 ==========
            if (supplier.IsDel == true)
                return BadRequest(ApiResult.Fail("供应商当前不可使用"));

            supplier.SupplierName = updateSupplierDto.supplierName;
            supplier.People = updateSupplierDto.people;
            supplier.PhoneNumber = updateSupplierDto.phoneNumber;
            supplier.Address = updateSupplierDto.address;
            supplier.Memo = updateSupplierDto.memo;

            await _context.SaveChangesAsync();

            // ========== 返回结果 ==========
            return Ok(ApiResult.Ok("供应商信息修改成功"));
        }


        [HttpPost]
        public async Task<ActionResult<ApiResult>> UpdateSupplierStatus(UpdateSupplierStatusDto dto)
        {
            //当前端单击停用的时候，已经传入isdel为true了
            //Console.WriteLine("传入的是" + dto.isDel + "===================" + !dto.isDel + "=========");// + supplier.IsDel);

            // ========== 参数校验 ==========
            if (string.IsNullOrWhiteSpace(dto.supplierID))
                return BadRequest(ApiResult.Fail("供应商ID不能为空"));


            // ========== 查询供应商信息 ==========
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(o => o.SupplierID == dto.supplierID);// && !o.IsDel);

            if (supplier == null)
                return NotFound(ApiResult.Fail("供应商不存在"));

            //// ========== 状态校验 ==========
            //if (supplier.IsDel == true)
            //    return BadRequest(ApiResult.Fail("供应商当前不可使用"));

            
            
            supplier.IsDel = dto.isDel;
            await _context.SaveChangesAsync();

            // ========== 返回结果 ==========
            return Ok(ApiResult.Ok("供应商状态已修改成功"));
        }
    }
}
