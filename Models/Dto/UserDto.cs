namespace backend.Models.Dto
{
    // ========== 用户相关 DTO ==========
    public class IdRequest
    {
        public string Id { get; set; }
    }
    /// <summary>
    /// 创建用户请求
    /// </summary>
    public class CreateUserRequest
    {
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 用户响应 DTO（脱敏）
    /// </summary>
    public class UserResponse
    {
        public string UserID { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsDel { get; set; }
        public string? Memo { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateUserRequest
    {
        public string ID { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string? Password { get; set; }
        public string? Memo { get; set; }

    }

    // ========== 角色相关 DTO ==========

    /// <summary>
    /// 创建角色请求
    /// </summary>
    public class CreateRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 角色响应 DTO
    /// </summary>
    public class RoleResponse
    {
        public string RoleID { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsDel { get; set; }
        public string? Memo { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public int UserCount { get; set; }
    }

    // ========== 用户角色关联 DTO ==========

    /// <summary>
    /// 用户角色关联请求
    /// </summary>
    public class UserRoleRequest
    {
        public string UserID { get; set; } = string.Empty;
        public string RoleID { get; set; } = string.Empty;
    }

    // UserRoleResponse
    public class UserRoleResponse
    {
        public string UserRoleID { get; set; }
        public string UserID { get; set; }
        public string RoleID { get; set; }
    }

    public class UpdateRoleStatusRequest
    {
        public string RoleId { get; set; }
        public bool IsDel { get; set; }
    }



    
}
