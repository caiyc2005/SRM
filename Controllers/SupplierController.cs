using backend.Models;
using backend.Models.Dto;
using backend.Models.Dto.Supplier;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    
    public class SupplierController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;

        public SupplierController(AppDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        /// <summary>
        /// 查询供应商信息表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "admin,purchase")]
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
        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
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
                    Password = _passwordService.HashPassword("123456"),
                    IsDel = false,
                    CreateTime = DateTime.Now,
                    Memo = $"供应商 - {addSupplierDto.supplierName}"
                };
                _context.Users.Add(user);
                newUserId = user.UserID;
            }
            else
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserCode == addSupplierDto.supplierCode);
                newUserId = existingUser?.UserID;
            }

            //关联UserID到Supplier供应商表里
            supplier.UserID = newUserId;

            // ========== 写入供应商用户关联（主账号） ==========
            if (!string.IsNullOrEmpty(newUserId))
            {
                var supplierUser = new SupplierUser
                {
                    SupplierUserID = Guid.NewGuid().ToString(),
                    SupplierID = supplier.SupplierID,
                    UserID = newUserId,
                    IsMainAccount = true,
                    CreatedAt = DateTime.Now
                };
                _context.SupplierUsers.Add(supplierUser);
            }

            await _context.SaveChangesAsync();

            var supplierRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "supplier" && !r.IsDel);
            if (supplierRole == null)
            {
                supplierRole = new Role
                {
                    RoleID = Guid.NewGuid().ToString(),
                    RoleName = "supplier",
                    IsDel = false,
                    CreateTime = DateTime.Now,
                    Memo = "供应商"
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

        /// <summary>
        /// 创建供应商子账号
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,supplier")]
        public async Task<ActionResult<ApiResult>> CreateSubAccount(CreateSubAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierID))
                return BadRequest(ApiResult.Fail("供应商ID不能为空"));
            if (string.IsNullOrWhiteSpace(dto.UserCode))
                return BadRequest(ApiResult.Fail("账号不能为空"));
            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest(ApiResult.Fail("用户名不能为空"));

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierID == dto.SupplierID && !s.IsDel);
            if (supplier == null)
                return BadRequest(ApiResult.Fail("供应商不存在或已禁用"));

            // ========== 校验账号唯一性 ==========
            if (await _context.Users.AnyAsync(u => u.UserCode == dto.UserCode && !u.IsDel))
                return Conflict(ApiResult.Fail("账号已存在"));

            // ========== 创建用户 ==========
            var user = new User
            {
                UserID = Guid.NewGuid().ToString(),
                UserCode = dto.UserCode,
                UserName = dto.UserName,
                Password = _passwordService.HashPassword("123456"),
                IsDel = false,
                CreateTime = DateTime.Now,
                Memo = dto.Memo
            };
            _context.Users.Add(user);

            // ========== 绑定 supplier 角色 ==========
            var supplierRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "supplier" && !r.IsDel);
            if (supplierRole != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserRoleID = Guid.NewGuid().ToString(),
                    UserID = user.UserID,
                    RoleID = supplierRole.RoleID
                });
            }

            // ========== 写入供应商用户关联（子账号） ==========
            _context.SupplierUsers.Add(new SupplierUser
            {
                SupplierUserID = Guid.NewGuid().ToString(),
                SupplierID = supplier.SupplierID,
                UserID = user.UserID,
                IsMainAccount = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("子账号创建成功", new
            {
                user.UserID,
                user.UserCode,
                user.UserName
            }));
        }

        /// <summary>
        /// 查询当前供应商主账号下的所有关联用户（仅主账号可查）
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,supplier")]
        public async Task<ActionResult<ApiResult>> GetSupplierUsers()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return BadRequest(ApiResult.Fail("无法获取当前用户信息"));

            // 检查当前用户是否为 admin 角色
            bool isAdmin = await _context.UserRoles
                .Where(ur => ur.UserID == currentUserId)
                .Join(_context.Roles.Where(r => r.RoleName == "admin"),
                    ur => ur.RoleID,
                    r => r.RoleID,
                    (ur, r) => ur)
                .AnyAsync();

            var currentSupplierUser = await _context.SupplierUsers
                .FirstOrDefaultAsync(su => su.UserID == currentUserId);

            if (currentSupplierUser == null)
            {
                if (isAdmin)
                {
                    // admin 角色可以查看所有供应商的用户列表
                    var allUsers = await _context.SupplierUsers
                        .Join(_context.Users,
                            su => su.UserID,
                            u => u.UserID,
                            (su, u) => new SupplierUserItemDto
                            {
                                SupplierUserID = su.SupplierUserID,
                                UserID = su.UserID,
                                UserCode = u.UserCode,
                                UserName = u.UserName,
                                IsMainAccount = su.IsMainAccount,
                                CreatedAt = su.CreatedAt,
                                Memo = u.Memo
                            })
                        .OrderByDescending(u => u.IsMainAccount)
                        .ThenBy(u => u.CreatedAt)
                        .ToListAsync();

                    return Ok(ApiResult.Ok("查询成功", allUsers));
                }
                return BadRequest(ApiResult.Fail("当前用户不是供应商用户"));
            }

            if (!currentSupplierUser.IsMainAccount)
                return BadRequest(ApiResult.Fail("仅主账号可查询子账号列表"));

            var users = await _context.SupplierUsers
                .Where(su => su.SupplierID == currentSupplierUser.SupplierID)
                .Join(_context.Users,
                    su => su.UserID,
                    u => u.UserID,
                    (su, u) => new SupplierUserItemDto
                    {
                        SupplierUserID = su.SupplierUserID,
                        UserID = su.UserID,
                        UserCode = u.UserCode,
                        UserName = u.UserName,
                        IsMainAccount = su.IsMainAccount,
                        CreatedAt = su.CreatedAt,
                        Memo = u.Memo
                    })
                .OrderByDescending(u => u.IsMainAccount)
                .ThenBy(u => u.CreatedAt)
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", users));
        }
    }
}
