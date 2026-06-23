using System;
using System.Collections.Generic;

namespace backend;

/// <summary>
/// 用户表
/// </summary>
public partial class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// 工号
    /// </summary>
    public string UserCode { get; set; } = null!;

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    /// 用户密码
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 是否删除（false=未删除，true=已删除）
    /// </summary>
    public bool IsDel { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Memo { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
