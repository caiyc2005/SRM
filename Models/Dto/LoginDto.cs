namespace WebApplication1.Models.Dto
{
    /// <summary>
    /// 登录请求 DTO
    /// </summary>
    public class LoginRequest
    {
        public string UserCode { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 登录响应 DTO
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// 用户信息 DTO（脱敏响应）
    /// </summary>
    public class UserInfo
    {
        public string ID { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
