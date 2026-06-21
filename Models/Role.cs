using backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        [StringLength(50)]
        public string RoleID { get; set; }

        [StringLength(50)]
        [Required]
        public string RoleName { get; set; }

        [Required]
        public bool IsDel { get; set; }

        public DateTime? CreateTime { get; set; } = DateTime.Now;

        public DateTime? UpdateTime { get; set; }

        [StringLength(50)]
        public string? Memo { get; set; }

        // 导航属性：一个角色有多个用户关联
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}
