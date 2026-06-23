using System;
using System.Collections.Generic;

namespace backend;

/// <summary>
/// 角色表
/// </summary>
public partial class Role
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public string RoleId { get; set; } = null!;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

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
