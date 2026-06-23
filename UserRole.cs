using System;
using System.Collections.Generic;

namespace backend;

/// <summary>
/// 用户角色表
/// </summary>
public partial class UserRole
{
    /// <summary>
    /// 用户角色ID
    /// </summary>
    public string UserRoleId { get; set; } = null!;

    /// <summary>
    /// 用户ID（外键，关联User表）
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// 角色ID（外键，关联Role表）
    /// </summary>
    public string RoleId { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
