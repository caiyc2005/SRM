using backend.Models;
using backend.Models.Dto;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public LoginController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求（账号、密码）</param>
        /// <returns>登录结果，包含 Token 和用户信息</returns>
        [Route("/login")]
        [HttpPost]
        
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
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
            Console.WriteLine($"传入的 UserCode: '{request.UserCode}'");
            Console.WriteLine($"长度: {request.UserCode.Length}");

            // 看看数据库里有什么
            var allUsers = await _context.Users
                .Select(u => new { u.UserCode, u.UserName })
                .ToListAsync();

            foreach (var u in allUsers)
            {
                Console.WriteLine($"数据库 UserCode: '{u.UserCode}'");
            }

            // 查找用户（包含角色导航）
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserCode == request.UserCode);//&& !u.IsDel);

            if (user == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "账号或密码错误1"
                });
            }

            // 验证密码（明文比对，后续可改为哈希比对）
            if (user.Password != request.Password)
            {
                //return Unauthorized(new LoginResponse
                //{
                //    Success = false,
                //    Message = "账号或密码错误"
                //});
                return Ok(new LoginResponse
                {
                    Success = false,
                    Message = "账号或密码错误2",
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
                    Roles = roleNames
                }
            });
        }
    }
}
