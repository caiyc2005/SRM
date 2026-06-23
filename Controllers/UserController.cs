using backend.Models;
using backend.Models.Dto;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;

        public UserController(AppDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        //临时调试

        [HttpPut]
        public async Task<ActionResult<ApiResult>> ToggleUserStatus([FromBody] IdRequest request)
        {
            var user = await _context.Users.FindAsync(request.Id);
            if (user == null) return NotFound(ApiResult.Fail("用户不存在"));
            user.IsDel = !user.IsDel;
            user.UpdateTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(ApiResult.Ok(user.IsDel ? "已禁用" : "已启用"));
        }

        [HttpPut]
        public async Task<ActionResult<ApiResult>> ToggleRoleStatus([FromBody] IdRequest request)
        {
            var role = await _context.Roles.FindAsync(request.Id);
            if (role == null) return NotFound(ApiResult.Fail("角色不存在"));
            role.IsDel = !role.IsDel;
            role.UpdateTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(ApiResult.Ok(role.IsDel ? "已禁用" : "已启用"));
        }


        // ==================== 用户管理 ====================

        /// <summary>
        /// 添加用户
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> AddUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserCode))
                return BadRequest(ApiResult.Fail("账号不能为空"));

            if (string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest(ApiResult.Fail("用户名不能为空"));

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ApiResult.Fail("密码不能为空"));

            // 检查账号是否已存在
            var exists = await _context.Users.AnyAsync(u => u.UserCode == request.UserCode);
            if (exists)
                return Conflict(ApiResult.Fail("账号已存在"));

            var user = new User
            {
                UserID = Guid.NewGuid().ToString(),
                UserCode = request.UserCode,
                UserName = request.UserName,
                Password = _passwordService.HashPassword(request.Password),
                IsDel = false,
                CreateTime = DateTime.Now,
                Memo = request.Memo
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("用户添加成功", new UserResponse
            {
                UserID = user.UserID,
                UserCode = user.UserCode,
                UserName = user.UserName,
                IsDel = user.IsDel,
                Memo = user.Memo,
                CreateTime = user.CreateTime,
                UpdateTime = user.UpdateTime
            }));
        }

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        //[HttpDelete("{id}")]
        [HttpPost]
        public async Task<ActionResult<ApiResult>> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(ApiResult.Fail("用户不存在"));

            user.IsDel = true;
            user.UpdateTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("用户已删除（软删除）"));
        }

        [HttpPut]
        public async Task<ActionResult<ApiResult>> UpdateUser([FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
                return NotFound(ApiResult.Fail("用户不存在"));

            user.UserCode = request.UserCode ?? user.UserCode;
            user.UserName = request.UserName ?? user.UserName;
            
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.Password = _passwordService.HashPassword(request.Password);
            user.Memo = request.Memo ?? user.Memo;
            user.UpdateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("用户修改成功", new UserResponse
            {
                UserID = user.UserID,
                UserCode = user.UserCode,
                UserName = user.UserName,
                IsDel = user.IsDel,
                Memo = user.Memo,
                CreateTime = user.CreateTime,
                UpdateTime = user.UpdateTime
            }));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetUsers()
        {
            var users = await _context.Users
                //.Where(u => !u.IsDel)
                .Select(u => new UserResponse
                {
                    UserID = u.UserID,
                    UserCode = u.UserCode,
                    UserName = u.UserName,
                    IsDel = u.IsDel,
                    Memo = u.Memo,
                    CreateTime = u.CreateTime,
                    UpdateTime = u.UpdateTime
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", users));
        }

        // ==================== 角色管理 ====================

        /// <summary>
        /// 添加角色
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> AddRole([FromBody] CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
                return BadRequest(ApiResult.Fail("角色名称不能为空"));

            // 检查角色名是否已存在
            var exists = await _context.Roles.AnyAsync(r => r.RoleName == request.RoleName);
            if (exists)
                return Conflict(ApiResult.Fail("角色名称已存在"));

            var role = new Role
            {
                RoleID = Guid.NewGuid().ToString(),
                RoleName = request.RoleName,
                IsDel = false,
                CreateTime = DateTime.Now,
                Memo = request.Memo
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("角色添加成功", new RoleResponse
            {
                RoleID = role.RoleID,
                RoleName = role.RoleName,
                IsDel = role.IsDel,
                Memo = role.Memo,
                CreateTime = role.CreateTime,
                UpdateTime = role.UpdateTime
            }));
        }

        /// <summary>
        /// 删除角色（物理删除），角色内有用户时不可删除
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> DeleteRole(string id)
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.RoleID == id);

            if (role == null)
                return NotFound(ApiResult.Fail("角色不存在"));

            if (role.UserRoles != null && role.UserRoles.Count != 0)
                return BadRequest(ApiResult.Fail($"该角色下存在 {role.UserRoles.Count} 个用户，请先移除所有用户后再删除"));

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("角色已删除"));
        }

        /// <summary>
        /// 启用角色（将 IsDel 设为 false）
        /// </summary>
        //[HttpPut("UpdateRoleStatus")]
        [HttpPut]
        public async Task<ActionResult<ApiResult>> UpdateRoleStatus([FromBody] UpdateRoleStatusRequest request)
        {
            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
                return NotFound(ApiResult.Fail("角色不存在"));

            role.IsDel = request.IsDel;
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("操作成功"));
        }


        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetRoles()
        {
            var roles = await _context.Roles
                //.Where(r => !r.IsDel)
                .Select(r => new RoleResponse
                {
                    RoleID = r.RoleID,
                    RoleName = r.RoleName,
                    IsDel = r.IsDel,
                    Memo = r.Memo,
                    CreateTime = r.CreateTime,
                    UpdateTime = r.UpdateTime
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", roles));
        }

        

        // ==================== 用户-角色关联 ====================

        [HttpGet]
        public async Task<ActionResult<ApiResult>> GetUserRoles()
        {
            var userRoles = await _context.UserRoles
                .Select(ur => new UserRoleResponse
                {
                    UserRoleID = ur.UserRoleID,
                    UserID = ur.UserID,
                    RoleID = ur.RoleID
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", userRoles));
        }

        /// <summary>
        /// 将用户添加到角色
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResult>> AddUserToRole([FromBody] UserRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.userID))
                return BadRequest(ApiResult.Fail("用户ID不能为空"));

            if (string.IsNullOrWhiteSpace(request.roleID))
                return BadRequest(ApiResult.Fail("角色ID不能为空"));

            // 检查用户是否存在
            var user = await _context.Users.FindAsync(request.userID);
            if (user == null)
                return NotFound(ApiResult.Fail("用户不存在"));

            // 检查角色是否存在
            var role = await _context.Roles.FindAsync(request.roleID);
            if (role == null)
                return NotFound(ApiResult.Fail("角色不存在"));

            // 检查是否已存在关联
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.UserID == request.userID && ur.RoleID == request.roleID);
            if (exists)
                return Conflict(ApiResult.Fail("该用户已在此角色中"));

            var userRole = new UserRole
            {
                UserRoleID = Guid.NewGuid().ToString(),
                UserID = request.userID,
                RoleID = request.roleID
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("用户已添加到角色"));
        }

        /// <summary>
        /// 将用户从角色移除
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult<ApiResult>> RemoveUserFromRole([FromBody] UserRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.userID))
                return BadRequest(ApiResult.Fail("用户ID不能为空"));

            if (string.IsNullOrWhiteSpace(request.roleID))
                return BadRequest(ApiResult.Fail("角色ID不能为空"));

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserID == request.userID && ur.RoleID == request.roleID);

            if (userRole == null)
                return NotFound(ApiResult.Fail("该用户不在指定角色中"));

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(ApiResult.Ok("用户已从角色移除"));
        }
    }
}
