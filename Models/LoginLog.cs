using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("LoginLog")]
    public class LoginLog
    {
        [Key]
        [Column("LoginLogID")]
        [StringLength(50)]
        public string LoginLogID { get; set; } = string.Empty;

        /// <summary>
        /// 注意：数据库中 User.UserID 为 varchar(50)，此处必须用同类型才能建外键
        /// </summary>
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string UserID { get; set; } = string.Empty;

        [StringLength(50)]
        [Required]
        public string UserCode { get; set; } = string.Empty;

        [StringLength(50)]
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 登录IP地址
        /// </summary>
        [StringLength(50)]
        public string? IPAddress { get; set; }

        // 导航属性
        public virtual User? User { get; set; }
    }
}
