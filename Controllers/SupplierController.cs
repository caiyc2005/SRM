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

        [HttpPost]
        public async Task<ActionResult<ApiResult>> AddSupplier(AddSupplierDto addSupplierDto)
        {
            if (string.IsNullOrWhiteSpace(addSupplierDto.supplierCode))
                return BadRequest(ApiResult.Fail("供应商编码不能为空"));
            if (string.IsNullOrWhiteSpace(addSupplierDto.supplierName))
                return BadRequest(ApiResult.Fail("供应商名称不能为空"));
            if (string.IsNullOrWhiteSpace(addSupplierDto.people))
                return BadRequest(ApiResult.Fail("联系人不能为空"));
            if (string.IsNullOrWhiteSpace(addSupplierDto.phoneNumber))
                return BadRequest(ApiResult.Fail("联系电话不能为空"));

            var exists = await _context.Suppliers.AnyAsync(s => s.SupplierCode == addSupplierDto.supplierCode);
            if (exists)
                return Conflict(ApiResult.Fail("供应商编码已存在"));

            var supplier = new Supplier
            {
                SupplierID = Guid.NewGuid().ToString(),
                SupplierCode = addSupplierDto.supplierCode,
                SupplierName = addSupplierDto.supplierName,
                People = addSupplierDto.people,
                PhoneNumber = addSupplierDto.phoneNumber,
                Address = addSupplierDto.address,
                Memo = addSupplierDto.memo,
                IsDel = false
            };

            _context.Suppliers.Add(supplier);

            var userExists = await _context.Users.AnyAsync(u => u.UserCode == addSupplierDto.supplierCode);
            string newUserId = null;
            if (!userExists)
            {
                var user = new User
                {
                    UserID = Guid.NewGuid().ToString(),
                    UserCode = addSupplierDto.supplierCode,
                    UserName = addSupplierDto.supplierName,
                    Password = "123456",
                    IsDel = false,
                    CreateTime = DateTime.Now,
                    //Memo = $"供应商用户 - {addSupplierDto.supplierName}"
                };
                _context.Users.Add(user);
                newUserId = user.UserID;
            }
            else
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserCode == addSupplierDto.supplierCode);
                newUserId = existingUser?.UserID;
            }

            var supplierRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "supplier" && !r.IsDel);
            if (supplierRole == null)
            {
                supplierRole = new Role
                {
                    RoleID = Guid.NewGuid().ToString(),
                    RoleName = "supplier",
                    IsDel = false,
                    CreateTime = DateTime.Now,
                    Memo = "供应商角色"
                };
                _context.Roles.Add(supplierRole);
            }

            if (!string.IsNullOrEmpty(newUserId))
            {
                var userRoleExists = await _context.UserRoles.AnyAsync(ur => ur.UserID == newUserId && ur.RoleID == supplierRole.RoleID);
                if (!userRoleExists)
                {
                    var userRole = new UserRole
                    {
                        UserRoleID = Guid.NewGuid().ToString(),
                        UserID = newUserId,
                        RoleID = supplierRole.RoleID
                    };
                    _context.UserRoles.Add(userRole);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("供应商添加成功，用户已同步创建", new SupplierDto
            {
                supplierID = supplier.SupplierID,
                supplierCode = supplier.SupplierCode,
                supplierName = supplier.SupplierName,
                people = supplier.People,
                phoneNumber = supplier.PhoneNumber,
                address = supplier.Address,
                isDel = supplier.IsDel,
                //memo = supplier.Memo
            }));
        }
    }
}
