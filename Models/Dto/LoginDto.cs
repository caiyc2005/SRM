namespace backend.Models.Dto
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
        public string UserID { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string? SupplierID { get; set; }
        public bool IsMainAccount { get; set; }
    }

    /// <summary>
    /// 登录日志查询请求
    /// </summary>
    public class LoginLogQueryDto
    {
        public string? UserCode { get; set; }
        public string? UserName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 登录日志项
    /// </summary>
    public class LoginLogItemDto
    {
        public string LoginLogID { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public string? IPAddress { get; set; }
    }
}
