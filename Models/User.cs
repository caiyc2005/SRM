using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        [StringLength(50)]
        public string ID { get; set; }

        [StringLength(50)]
        [Required]
        public string UserCode { get; set; }

        [StringLength(50)]
        [Required]
        public string UserName { get; set; }

        [StringLength(50)]
        [Required]
        public string Password { get; set; }

        [Required]
        public bool IsDel { get; set; }

        public DateTime? CreateTime { get; set; }= DateTime.Now;

        public DateTime? UpdateTime { get; set; }

        [StringLength(50)]
        public string? Memo { get; set; }

        // 导航属性：一个用户有多个角色关联
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}
