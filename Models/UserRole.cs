using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models.User
{
    [Table("UserRole")]
    public class UserRole
    {
        [Key]
        [StringLength(50)]
        public string UserRoleID { get; set; }

        [StringLength(50)]
        [Required]
        public string UserID { get; set; }

        [StringLength(50)]
        [Required]
        public string RoleID { get; set; }

        // 外键关联：多个用户角色记录属于一个用户
        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; }

        // 外键关联：多个用户角色记录属于一个角色
        [ForeignKey(nameof(RoleID))]
        public virtual Role Role { get; set; }
    }
}
