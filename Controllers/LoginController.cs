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
    public class LoginController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;

        public LoginController(AppDbContext context, IJwtService jwtService, IPasswordService passwordService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordService = passwordService;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求（账号、密码）</param>
        /// <returns>登录结果，包含 Token 和用户信息</returns>
        [Route("/login")]
        [HttpPost]
        
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(request.UserCode))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "账号不能为空"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "密码不能为空"
                });
            }
            

            // 在查询前打印，看看到底传了什么
            //Console.WriteLine($"传入的 UserCode: '{request.UserCode}'");
            //Console.WriteLine($"长度: {request.UserCode.Length}");

            // 看看数据库里有什么
            var allUsers = await _context.Users
                .Select(u => new { u.UserCode, u.UserName })
                .ToListAsync();

            //foreach (var u in allUsers)
            //{
            //    Console.WriteLine($"数据库 UserCode: '{u.UserCode}'");
            //}

            // 查找用户（包含角色导航）
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(i => !i.IsDel)
                .FirstOrDefaultAsync(u => u.UserCode == request.UserCode);//&& !u.IsDel);
            //if (user.IsDel)
            //{
            //    return Ok(new LoginResponse
            //    {
            //        Success = false,
            //        Message = "当前账号无权限登录",
            //    });
            //}

            if (user == null)
            {
                return Ok(new LoginResponse
                {
                    Success = false,
                    Message = "账号或密码错误",
                });
            }

            // 验证密码（密文比对）
            if (!_passwordService.VerifyPassword(user.Password, request.Password))
            {
                return Ok(new LoginResponse
                {
                    Success = false,
                    Message = "账号或密码错误",
                });
            }

            // 获取用户角色名称列表
            var roleNames = user.UserRoles?
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.RoleName)
                .ToList() ?? new List<string>();

            // 生成 JWT Token
            var token = _jwtService.GenerateToken(
                userId: user.UserID,
                userName: user.UserName,
                userCode: user.UserCode,
                roles: roleNames
            );
            // ========== 如果是供应商角色，查 SupplierID 和 IsMainAccount ==========
            // 如果同时有 admin 角色，优先以 admin 身份登录，不读取供应商信息
            string? supplierID = null;
            var isMainAccount = false;
            if (roleNames.Contains("supplier") && !roleNames.Contains("admin"))
            {
                var supplierUser = await _context.SupplierUsers
                    .FirstOrDefaultAsync(su => su.UserID == user.UserID);
                if (supplierUser != null)
                {
                    supplierID = supplierUser.SupplierID;
                    isMainAccount = supplierUser.IsMainAccount;
                }
            }

            // ========== 记录登录日志 ==========
            // 优先取反向代理 X-Forwarded-For，再取直连 IP，IPv6 回环转成 127.0.0.1
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (ipAddress == "::1")
                ipAddress = "127.0.0.1";

            var loginLog = new LoginLog
            {
                LoginLogID = Guid.NewGuid().ToString(),
                UserID = user.UserID,
                UserCode = user.UserCode,
                UserName = user.UserName,
                LoginTime = DateTime.Now,
                IPAddress = ipAddress
            };
            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            // 返回
            return Ok(new LoginResponse
            {
                Success = true,
                Message = "登录成功",
                Token = token,
                User = new UserInfo
                {
                    UserID = user.UserID,
                    UserCode = user.UserCode,
                    UserName = user.UserName,
                    Roles = roleNames,
                    SupplierID = supplierID,
                    IsMainAccount = isMainAccount
                }
            });
        }

        /// <summary>
        /// 查询登录日志（仅 admin）
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ApiResult>> GetLoginLogs(LoginLogQueryDto queryDto)
        {
            var queryable = _context.LoginLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryDto.UserCode))
                queryable = queryable.Where(l => l.UserCode.Contains(queryDto.UserCode));

            if (!string.IsNullOrWhiteSpace(queryDto.UserName))
                queryable = queryable.Where(l => l.UserName.Contains(queryDto.UserName));

            if (queryDto.StartTime.HasValue)
                queryable = queryable.Where(l => l.LoginTime >= queryDto.StartTime.Value);

            if (queryDto.EndTime.HasValue)
                queryable = queryable.Where(l => l.LoginTime <= queryDto.EndTime.Value.AddDays(1).AddTicks(-1));

            var total = await queryable.CountAsync();

            var list = await queryable
                .OrderByDescending(l => l.LoginTime)
                .Skip((queryDto.PageIndex - 1) * queryDto.PageSize)
                .Take(queryDto.PageSize)
                .Select(l => new LoginLogItemDto
                {
                    LoginLogID = l.LoginLogID,
                    UserID = l.UserID,
                    UserCode = l.UserCode,
                    UserName = l.UserName,
                    LoginTime = l.LoginTime,
                    IPAddress = l.IPAddress
                })
                .ToListAsync();

            return Ok(ApiResult.Ok("查询成功", new
            {
                Total = total,
                PageIndex = queryDto.PageIndex,
                PageSize = queryDto.PageSize,
                List = list
            }));
        }
    }
}
